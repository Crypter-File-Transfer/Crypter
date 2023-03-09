/*
 * Copyright (C) 2023 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Events;
using Crypter.Common.Client.Interfaces.Models;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.KeyExchange;
using System;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations
{
   public class UserKeysService : IUserKeysService, IDisposable
   {
      private readonly ICrypterApiClient _crypterApiClient;
      private readonly ICryptoProvider _cryptoProvider;
      private readonly IUserPasswordService _userPasswordService;
      private readonly IUserKeysRepository _userKeysRepository;
      private readonly IUserSessionService _userSessionService;

      public Maybe<byte[]> MasterKey { get; protected set; } = Maybe<byte[]>.None;
      public Maybe<byte[]> PrivateKey { get; protected set; } = Maybe<byte[]>.None;

      public UserKeysService(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserPasswordService userPasswordService, IUserKeysRepository userKeysRepository, IUserSessionService userSessionService)
      {
         _crypterApiClient = crypterApiClient;
         _userPasswordService = userPasswordService;
         _cryptoProvider = cryptoProvider;
         _userKeysRepository = userKeysRepository;
         _userSessionService = userSessionService;

         _userSessionService.ServiceInitializedEventHandler += InitializeAsync;
         _userSessionService.UserLoggedOutEventHandler += Recycle;
      }

      public async void InitializeAsync(object sender, UserSessionServiceInitializedEventArgs args)
      {
         if (args.IsLoggedIn)
         {
            MasterKey = await _userKeysRepository.GetMasterKeyAsync();
            PrivateKey = await _userKeysRepository.GetPrivateKeyAsync();
         }
      }

      #region Download Existing Keys

      public Task DownloadExistingKeysAsync(Username username, Password password, bool trustDevice)
      {
         return _userPasswordService.DeriveUserCredentialKeyAsync(username, password, _userPasswordService.CurrentPasswordVersion)
            .IfSomeAsync(credentialKey => DownloadExistingKeysAsync(credentialKey, trustDevice));
      }

      public Task DownloadExistingKeysAsync(byte[] credentialKey, bool trustDevice)
      {
         return DownloadAndDecryptMasterKey(credentialKey)
            .BindAsync(masterKey => DownloadAndDecryptPrivateKey(masterKey)
               .IfSomeAsync(privateKey => StoreSecretKeys(masterKey, privateKey, trustDevice)));
      }

      private Task<Maybe<byte[]>> DownloadAndDecryptMasterKey(byte[] credentialKey)
      {
         return _crypterApiClient.UserKey.GetMasterKeyAsync()
            .MapAsync<GetMasterKeyError, GetMasterKeyResponse, byte[]>(x => _cryptoProvider.Encryption.Decrypt(credentialKey, x.Nonce, x.EncryptedKey))
            .ToMaybeTask();
      }

      private Task<Maybe<byte[]>> DownloadAndDecryptPrivateKey(byte[] masterKey)
      {
         return _crypterApiClient.UserKey.GetPrivateKeyAsync()
            .MapAsync<GetPrivateKeyError, GetPrivateKeyResponse, byte[]>(x => _cryptoProvider.Encryption.Decrypt(masterKey, x.Nonce, x.EncryptedKey))
            .ToMaybeTask();
      }

      #endregion

      #region Upload New Keys

      public Task<Maybe<RecoveryKey>> UploadNewKeysAsync(Username username, Password password, VersionedPassword versionedPassword, bool trustDevice)
      {
         return _userPasswordService.DeriveUserCredentialKeyAsync(username, password, _userPasswordService.CurrentPasswordVersion)
            .BindAsync(credentialKey => UploadNewKeysAsync(username, versionedPassword, credentialKey, trustDevice));
      }

      public Task<Maybe<RecoveryKey>> UploadNewKeysAsync(Username username, VersionedPassword versionedPassword, byte[] credentialKey, bool trustDevice)
      {
         return UploadNewMasterKeyAsync(credentialKey, username, versionedPassword)
            .BindAsync(recoveryKey => UploadNewUserKeyPairAsync(recoveryKey.MasterKey)
               .MapAsync(privateKey => StoreSecretKeys(recoveryKey.MasterKey, privateKey, trustDevice))
               .MapAsync(_ => recoveryKey));
      }

      private Task<Maybe<RecoveryKey>> UploadNewMasterKeyAsync(byte[] credentialKey, Username username, VersionedPassword versionedPassword)
      {
         byte[] newMasterKey = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.KeySize);
         byte[] nonce = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.NonceSize);
         byte[] encryptedMasterKey = _cryptoProvider.Encryption.Encrypt(credentialKey, nonce, newMasterKey);
         byte[] recoveryProof = _cryptoProvider.Random.GenerateRandomBytes(32);

         return _crypterApiClient.UserKey.InsertMasterKeyAsync(new InsertMasterKeyRequest(username, versionedPassword.Password, encryptedMasterKey, nonce, recoveryProof))
            .ToMaybeTask()
            .MapAsync(x => new RecoveryKey(newMasterKey, recoveryProof));
      }

      private Task<Maybe<byte[]>> UploadNewUserKeyPairAsync(byte[] masterKey)
      {
         X25519KeyPair keyPair = _cryptoProvider.KeyExchange.GenerateKeyPair();
         byte[] nonce = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.NonceSize);
         byte[] encryptedPrivateKey = _cryptoProvider.Encryption.Encrypt(masterKey, nonce, keyPair.PrivateKey);

         InsertKeyPairRequest request = new InsertKeyPairRequest(encryptedPrivateKey, keyPair.PublicKey, nonce);
         return _crypterApiClient.UserKey.InsertKeyPairAsync(request)
            .ToMaybeTask()
            .MapAsync(x => keyPair.PrivateKey);
      }

      #endregion

      public Task<Maybe<RecoveryKey>> GetExistingRecoveryKeyAsync(Username username, Password password)
      {
         return MasterKey
            .BindAsync(masterKey => _userPasswordService.DeriveUserAuthenticationPasswordAsync(username, password, _userPasswordService.CurrentPasswordVersion)
            .BindAsync(versionedPassword => GetExistingRecoveryKeyAsync(username, versionedPassword)));
      }

      public Task<Maybe<RecoveryKey>> GetExistingRecoveryKeyAsync(Username username, VersionedPassword versionedPassword)
      {
         return MasterKey
            .BindAsync(masterKey =>
            {
               GetMasterKeyRecoveryProofRequest request = new GetMasterKeyRecoveryProofRequest(username, versionedPassword.Password);
               return _crypterApiClient.UserKey.GetMasterKeyRecoveryProofAsync(request).ToMaybeTask()
                  .MapAsync(x => new RecoveryKey(masterKey, x.Proof));
            });
      }

      private async Task<Unit> StoreSecretKeys(byte[] masterKey, byte[] privateKey, bool trustDevice)
      {
         MasterKey = masterKey;
         PrivateKey = privateKey;
         await _userKeysRepository.StoreMasterKeyAsync(masterKey, trustDevice);
         await _userKeysRepository.StorePrivateKeyAsync(privateKey, trustDevice);
         return Unit.Default;
      }

      private void Recycle(object sender, EventArgs _)
      {
         MasterKey = Maybe<byte[]>.None;
         PrivateKey = Maybe<byte[]>.None;
      }

      public void Dispose()
      {
         _userSessionService.ServiceInitializedEventHandler -= InitializeAsync;
         _userSessionService.UserLoggedOutEventHandler -= Recycle;
         GC.SuppressFinalize(this);
      }
   }
}
