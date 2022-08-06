/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Events;
using Crypter.ClientServices.Interfaces.Repositories;
using Crypter.ClientServices.Models;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication;
using Crypter.Contracts.Features.Keys;
using Crypter.CryptoLib.Models;
using Crypter.CryptoLib.SodiumLib;
using System;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class UserKeysService : IUserKeysService, IDisposable
   {
      private readonly ICrypterApiService _crypterApiService;
      private readonly IUserKeysRepository _userKeysRepository;
      private readonly IUserSessionService _userSessionService;
      private readonly IUserPasswordService _userPasswordService;

      public Maybe<byte[]> MasterKey { get; protected set; }
      public Maybe<byte[]> PrivateKey { get; protected set; }

      public UserKeysService(
         ICrypterApiService crypterApiService,
         IUserKeysRepository userKeysRepository,
         IUserSessionService userSessionService,
         IUserPasswordService userPasswordService)
      {
         _crypterApiService = crypterApiService;
         _userKeysRepository = userKeysRepository;
         _userSessionService = userSessionService;
         _userPasswordService = userPasswordService;

         _userSessionService.ServiceInitializedEventHandler += OnUserSessionServiceInitialized;
         _userSessionService.UserLoggedInEventHandler += OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler += OnUserLoggedOut;
      }

      private async Task InitializeAsync(Username username, Password password, bool rememberUser)
      {
         Task<Maybe<byte[]>> HandlePrivateKeyDownloadErrorAsync(GetPrivateKeyError error, byte[] masterKey)
         {
#pragma warning disable CS8524
            return error switch
            {
               GetPrivateKeyError.NotFound => UploadNewUserKeyPairAsync(masterKey),
               GetPrivateKeyError.UnkownError => Maybe<byte[]>.None.AsTask()
            };
#pragma warning restore CS8524
         }

         Task<Maybe<byte[]>> HandleMasterKeyDownloadErrorAsync(GetMasterKeyError error, byte[] userCredentialKey, Username username, Password password)
         {
            VersionedPassword versionedPassword = _userPasswordService.DeriveUserAuthenticationPassword(username, password, _userPasswordService.CurrentPasswordVersion);
#pragma warning disable CS8524
            return error switch
            {
               GetMasterKeyError.NotFound => UploadNewMasterKeyAsync(userCredentialKey, username, AuthenticationPassword.From(versionedPassword.Password)),
               GetMasterKeyError.UnknownError => Maybe<byte[]>.None.AsTask()
            };
#pragma warning restore CS8524
         }

         byte[] credentialKey = _userPasswordService.DeriveUserCredentialKey(username, password, _userPasswordService.CurrentPasswordVersion);

         MasterKey = await _crypterApiService.GetMasterKeyAsync()
            .MatchAsync(
               async downloadError => await HandleMasterKeyDownloadErrorAsync(downloadError, credentialKey, username, password),
               encryptedKeyInfo => DecryptMasterKey(encryptedKeyInfo, credentialKey),
               Maybe<byte[]>.None);

         await MasterKey.IfSomeAsync(async masterKey =>
         {
            PrivateKey = await _crypterApiService.GetPrivateKeyAsync().MatchAsync(
               async downloadError => await HandlePrivateKeyDownloadErrorAsync(downloadError, masterKey),
               encryptedKeyInfo => DecodeAndDecryptUserPrivateKey(encryptedKeyInfo.Key, encryptedKeyInfo.Nonce, masterKey),
               Maybe<byte[]>.None);
         });

         await PrivateKey.IfSomeAsync(async x => await _userKeysRepository.StorePrivateKeyAsync(x, rememberUser));
      }

      public async Task<Maybe<byte[]>> GetUserMasterKeyAsync(Username username, Password password)
      {
         byte[] credentialKey = _userPasswordService.DeriveUserCredentialKey(username, password, _userPasswordService.CurrentPasswordVersion);
         var masterKeyResponse = await _crypterApiService.GetMasterKeyAsync();
         return masterKeyResponse.Match(
            Maybe<byte[]>.None,
            x => DecryptMasterKey(x, credentialKey));
      }

      public async Task<Maybe<RecoveryKey>> GetUserRecoveryKeyAsync(Username username, Password password)
      {
         return await GetUserMasterKeyAsync(username, password)
            .MatchAsync(
            () => Maybe<RecoveryKey>.None,
            async masterKey =>
            {
               VersionedPassword versionedPassword = _userPasswordService.DeriveUserAuthenticationPassword(username, password, _userPasswordService.CurrentPasswordVersion);
               var request = new GetMasterKeyRecoveryProofRequest(username, AuthenticationPassword.From(versionedPassword.Password));
               var recoveryProofResponse = await _crypterApiService.GetMasterKeyRecoveryProofAsync(request);

               return recoveryProofResponse.Match(
                  Maybe<RecoveryKey>.None,
                  recoveryProof => new RecoveryKey(masterKey, recoveryProof.RecoveryProof));
            });
      }

      private static Maybe<byte[]> DecryptMasterKey(GetMasterKeyResponse encryptedKeyInfo, byte[] userSymmetricKey)
      {
         byte[] encryptedKey = Convert.FromBase64String(encryptedKeyInfo.EncryptedKey);
         byte[] nonce = Convert.FromBase64String(encryptedKeyInfo.Nonce);
         EncryptedBox encryptedBox = new EncryptedBox(encryptedKey, nonce);
         try
         {
            return SecretBox.Open(encryptedBox, userSymmetricKey);
         }
         catch (Exception)
         {
            return Maybe<byte[]>.None;
         }
      }

      private Task<Maybe<byte[]>> UploadNewUserKeyPairAsync(byte[] masterKey)
      {
         AsymmetricKeyPair keyPair = PublicKeyBox.GenerateKeyPair();
         EncryptedBox encryptionInfo = SecretBox.Create(keyPair.PrivateKey, masterKey);

         return UploadKeyPairAsync(encryptionInfo.Contents, keyPair.PublicKey, encryptionInfo.Nonce)
            .ToMaybeTask()
            .BindAsync(x => Maybe<byte[]>.From(keyPair.PrivateKey).AsTask());
      }

      private Task<Maybe<byte[]>> UploadNewMasterKeyAsync(byte[] credentialKey, Username username, AuthenticationPassword password)
      {
         byte[] newMasterKey = SecretBox.GenerateKey();
         EncryptedBox encryptedBox = SecretBox.Create(newMasterKey, credentialKey);
         byte[] recoveryProof = CryptoLib.SodiumLib.Random.RandomBytes(16);

         string encodedEncryptedKey = Convert.ToBase64String(encryptedBox.Contents);
         string encodedNonce = Convert.ToBase64String(encryptedBox.Nonce);
         string encodedRecoveryProof = Convert.ToBase64String(recoveryProof);

         return _crypterApiService.InsertMasterKeyAsync(new InsertMasterKeyRequest(username, password, encodedEncryptedKey, encodedNonce, encodedRecoveryProof))
            .ToMaybeTask()
            .BindAsync(x => Maybe<byte[]>.From(newMasterKey).AsTask());
      }

      private static Maybe<byte[]> DecodeAndDecryptUserPrivateKey(string encryptedPrivateKey, string nonce, byte[] decryptionKey)
      {
         byte[] decodedPrivateKey = Convert.FromBase64String(encryptedPrivateKey);
         byte[] decodedNonce = Convert.FromBase64String(nonce);
         EncryptedBox encryptedBox = new EncryptedBox(decodedPrivateKey, decodedNonce);
         try
         {
            return SecretBox.Open(encryptedBox, decryptionKey);
         }
         catch (Exception)
         {
            return Maybe<byte[]>.None;
         }
      }

      private Task<Either<InsertKeyPairError, InsertKeyPairResponse>> UploadKeyPairAsync(byte[] encryptedPrivateKey, byte[] publicKey, byte[] nonce)
      {
         var encodedPublicKey = Convert.ToBase64String(publicKey);
         var encodedEncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var encodedNonce = Convert.ToBase64String(nonce);

         var request = new InsertKeyPairRequest(encodedEncryptedPrivateKey, encodedPublicKey, encodedNonce);
         return _crypterApiService.InsertDiffieHellmanKeysAsync(request);
      }

      private async void OnUserSessionServiceInitialized(object sender, UserSessionServiceInitializedEventArgs args)
      {
         if (args.IsLoggedIn)
         {
            PrivateKey = await _userKeysRepository.GetPrivateKeyAsync();
         }
      }

      private async void OnUserLoggedIn(object sender, UserLoggedInEventArgs args)
      {
         await InitializeAsync(args.Username, args.Password, args.RememberUser);
      }

      private void OnUserLoggedOut(object sender, EventArgs _)
      {
         MasterKey = Maybe<byte[]>.None;
         PrivateKey = Maybe<byte[]>.None;
      }

      public void Dispose()
      {
         MasterKey = Maybe<byte[]>.None;
         PrivateKey = Maybe<byte[]>.None;
         _userSessionService.UserLoggedInEventHandler -= OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler -= OnUserLoggedOut;
         GC.SuppressFinalize(this);
      }
   }
}
