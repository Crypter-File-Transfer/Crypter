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
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Keys;
using Crypter.CryptoLib.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class UserKeysService : IUserKeysService, IDisposable
   {
      private readonly ICrypterApiService _crypterApiService;
      private readonly ISimpleEncryptionService _simpleEncryptionService;
      private readonly IUserKeysRepository _userKeysRepository;
      private readonly IUserSessionService _userSessionService;

      public Maybe<PEMString> Ed25519PrivateKey { get; protected set; }
      public Maybe<PEMString> X25519PrivateKey { get; protected set; }

      public UserKeysService(ICrypterApiService crypterApiService, ISimpleEncryptionService simpleEncryptionService, IUserKeysRepository userKeysRepository, IUserSessionService userSessionService)
      {
         _crypterApiService = crypterApiService;
         _simpleEncryptionService = simpleEncryptionService;
         _userKeysRepository = userKeysRepository;
         _userSessionService = userSessionService;

         _userSessionService.ServiceInitializedEventHandler += OnUserSessionServiceInitialized;
         _userSessionService.UserLoggedInEventHandler += OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler += OnUserLoggedOut;
      }

      private async Task PrepareUserKeysOnUserLoginAsync(Username username, Password password, bool rememberUser)
      {
         Task<Maybe<PEMString>> HandlePrivateKeyDownloadErrorAsync(GetPrivateKeyError error, UserKeyType keyType, byte[] masterKey)
         {
            return error switch
            {
               GetPrivateKeyError.NotFound => UploadNewUserKeyPairAsync(keyType, masterKey),
               _ => Maybe<PEMString>.None.AsTask()
            };
         }

         Task<Maybe<byte[]>> HandleMasterKeyDownloadErrorAsync(GetMasterKeyError error, byte[] userCredentialKey)
         {
            return error switch
            {
               GetMasterKeyError.NotFound => UploadNewMasterKeyAsync(userCredentialKey),
               _ => Maybe<byte[]>.None.AsTask()
            };
         }

         byte[] credentialKey = CryptoLib.UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, password);

         var masterKeyResponse = await _crypterApiService.GetMasterKeyAsync();
         Maybe<byte[]> masterKey = await masterKeyResponse.MatchAsync(
            async downloadError => await HandleMasterKeyDownloadErrorAsync(downloadError, credentialKey),
            encryptedKeyInfo => DecryptMasterKey(encryptedKeyInfo, credentialKey),
            Maybe<byte[]>.None);

         var ed25519DownloadResult = await DownloadPrivateKeyAsync(UserKeyType.Ed25519);
         var x25519DownloadResult = await DownloadPrivateKeyAsync(UserKeyType.X25519);

         await masterKey.IfSomeAsync(async masterKey =>
         {
            Ed25519PrivateKey = await ed25519DownloadResult.MatchAsync(
               async downloadError => await HandlePrivateKeyDownloadErrorAsync(downloadError, UserKeyType.Ed25519, masterKey),
               encryptedKeyInfo => DecodeAndDecryptUserPrivateKey(encryptedKeyInfo.EncryptedKey, encryptedKeyInfo.ClientIV, masterKey),
               Maybe<PEMString>.None);

            X25519PrivateKey = await x25519DownloadResult.MatchAsync(
               async downloadError => await HandlePrivateKeyDownloadErrorAsync(downloadError, UserKeyType.X25519, masterKey),
               encryptedKeyInfo => DecodeAndDecryptUserPrivateKey(encryptedKeyInfo.EncryptedKey, encryptedKeyInfo.ClientIV, masterKey),
               Maybe<PEMString>.None);
         });

         await Ed25519PrivateKey.IfSomeAsync(async x => await _userKeysRepository.StoreEd25519PrivateKeyAsync(x, rememberUser));
         await X25519PrivateKey.IfSomeAsync(async x => await _userKeysRepository.StoreX25519PrivateKeyAsync(x, rememberUser));
      }

      public (PEMString PrivateKey, PEMString PublicKey) CreateX25519KeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDH.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);

         return (privateKey, publicKey);
      }

      public (PEMString PrivateKey, PEMString PublicKey) CreateEd25519KeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDSA.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);

         return (privateKey, publicKey);
      }

      public async Task<Maybe<byte[]>> GetUserMasterKeyAsync(Username username, Password password)
      {
         byte[] credentialKey = CryptoLib.UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, password);
         var masterKeyResponse = await _crypterApiService.GetMasterKeyAsync();
         return masterKeyResponse.Match(
            Maybe<byte[]>.None,
            x => DecryptMasterKey(x, credentialKey));
      }

      private Maybe<byte[]> DecryptMasterKey(GetMasterKeyResponse encryptedKeyInfo, byte[] userSymmetricKey)
      {
         byte[] encryptedKey = Convert.FromBase64String(encryptedKeyInfo.EncryptedKey);
         byte[] iv = Convert.FromBase64String(encryptedKeyInfo.ClientIV);
         try
         {
            return _simpleEncryptionService.Decrypt(key: userSymmetricKey, iv: iv, ciphertext: encryptedKey);
         }
         catch (Exception)
         {
            return Maybe<byte[]>.None;
         }
      }

      private Task<Maybe<PEMString>> UploadNewUserKeyPairAsync(UserKeyType keyType, byte[] masterKey)
      {
#pragma warning disable CS8524
         var (privateKey, publicKey) = keyType switch
         {
            UserKeyType.Ed25519 => CreateEd25519KeyPair(),
            UserKeyType.X25519 => CreateX25519KeyPair()
         };
#pragma warning restore CS8524

         var (encryptedPrivateKey, iv) = _simpleEncryptionService.Encrypt(masterKey, privateKey.Value);

         return UploadKeyPairAsync(encryptedPrivateKey, publicKey, iv, keyType)
            .ToMaybeTask()
            .BindAsync(x => Maybe<PEMString>.From(privateKey).AsTask());
      }

      private Task<Maybe<byte[]>> UploadNewMasterKeyAsync(byte[] userSymmetricKey)
      {
         byte[] newMasterKey = _simpleEncryptionService.GenerateKey();
         var (encryptedKey, iv) = _simpleEncryptionService.Encrypt(key: userSymmetricKey, plaintext: newMasterKey);

         string encodedEncryptedKey = Convert.ToBase64String(encryptedKey);
         string encodedIV = Convert.ToBase64String(iv);

         return _crypterApiService.InsertMasterKeyAsync(new InsertMasterKeyRequest(encodedEncryptedKey, encodedIV))
            .ToMaybeTask()
            .BindAsync(x => Maybe<byte[]>.From(newMasterKey).AsTask());
      }

      private Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> DownloadPrivateKeyAsync(UserKeyType keyType)
      {
         return keyType switch
         {
            UserKeyType.Ed25519 => _crypterApiService.GetDigitalSignaturePrivateKeyAsync(),
            UserKeyType.X25519 => _crypterApiService.GetDiffieHellmanPrivateKeyAsync(),
            _ => throw new NotImplementedException("Unknown key type.")
         };
      }

      private Maybe<PEMString> DecodeAndDecryptUserPrivateKey(string encryptedPrivateKey, string iv, byte[] decryptionKey)
      {
         byte[] decodedPrivateKey = Convert.FromBase64String(encryptedPrivateKey);
         byte[] decodedIV = Convert.FromBase64String(iv);
         try
         {
            string pemString = _simpleEncryptionService.DecryptToString(key: decryptionKey, iv: decodedIV, ciphertext: decodedPrivateKey);
            return PEMString.From(pemString);
         }
         catch (Exception)
         {
            return Maybe<PEMString>.None;
         }
      }

      private Task<Either<InsertKeyPairError, InsertKeyPairResponse>> UploadKeyPairAsync(byte[] encryptedPrivateKey, PEMString publicKey, byte[] iv, UserKeyType keyType)
      {
         var base64PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey.Value));
         var base64EncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var base64IV = Convert.ToBase64String(iv);

         var request = new InsertKeyPairRequest(base64EncryptedPrivateKey, base64PublicKey, base64IV);
         return keyType switch
         {
            UserKeyType.Ed25519 => _crypterApiService.InsertDigitalSignatureKeysAsync(request),
            UserKeyType.X25519 => _crypterApiService.InsertDiffieHellmanKeysAsync(request),
            _ => throw new NotImplementedException("Unknown key type.")
         };
      }

      private async void OnUserSessionServiceInitialized(object sender, UserSessionServiceInitializedEventArgs args)
      {
         if (args.IsLoggedIn)
         {
            Ed25519PrivateKey = await _userKeysRepository.GetEd25519PrivateKeyAsync();
            X25519PrivateKey = await _userKeysRepository.GetX25519PrivateKeyAsync();
         }
      }

      private async void OnUserLoggedIn(object sender, UserLoggedInEventArgs args)
      {
         await PrepareUserKeysOnUserLoginAsync(args.Username, args.Password, args.RememberUser);
      }

      private void OnUserLoggedOut(object sender, EventArgs _)
      {
         Ed25519PrivateKey = Maybe<PEMString>.None;
         X25519PrivateKey = Maybe<PEMString>.None;
      }

      public void Dispose()
      {
         _userSessionService.UserLoggedInEventHandler -= OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler -= OnUserLoggedOut;
         GC.SuppressFinalize(this);
      }
   }
}
