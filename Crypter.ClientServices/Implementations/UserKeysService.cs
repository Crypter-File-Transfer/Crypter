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
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.User.GetPrivateKey;
using Crypter.Contracts.Features.User.UpdateKeys;
using Crypter.CryptoLib.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Implementations
{
   public class UserKeysService : IUserKeysService
   {
      private readonly ICrypterApiService _crypterApiService;
      private readonly ISimpleEncryptionService _simpleEncryptionService;
      private readonly IUserKeysRepository _userKeysRepository;

      public Maybe<PEMString> Ed25519PrivateKey { get; protected set; } = Maybe<PEMString>.None;

      public Maybe<PEMString> X25519PrivateKey { get; protected set; } = Maybe<PEMString>.None;

      public UserKeysService(ICrypterApiService crypterApiService, ISimpleEncryptionService simpleEncryptionService, IUserKeysRepository userKeysRepository)
      {
         _crypterApiService = crypterApiService;
         _simpleEncryptionService = simpleEncryptionService;
         _userKeysRepository = userKeysRepository;
      }

      public async Task InitializeAsync()
      {
         Ed25519PrivateKey = await _userKeysRepository.GetEd25519PrivateKeyAsync();
         X25519PrivateKey = await _userKeysRepository.GetX25519PrivateKeyAsync();
      }

      public async Task<bool> PrepareUserKeysOnUserLoginAsync(Username username, Password password, bool rememberUser)
      {
         async Task<Maybe<PEMString>> HandleDownloadErrorAsync(GetPrivateKeyError error, UserKeyType keyType, byte[] userSymmetricKey, bool rememberUser)
         {
            return error switch
            {
               GetPrivateKeyError.NotFound => await UploadNewUserKeyAsync(keyType, userSymmetricKey, rememberUser),
               _ => Maybe<PEMString>.None
            };
         }

         Maybe<PEMString> HandleDownloadSuccess(GetPrivateKeyResponse encryptedKeyInfo, UserKeyType keyType, byte[] userSymmetricKey)
         {
            Base64String encryptedKey = Base64String.From(encryptedKeyInfo.EncryptedKey);
            Base64String iv = Base64String.From(encryptedKeyInfo.IV);
            return DecryptUserPrivateKey(encryptedKey, iv, userSymmetricKey);
         }

         var symmetricKey = DeriveUserSymmetricKey(username, password);

         var ed25519DownloadResult = await DownloadPrivateKeyAsync(UserKeyType.Ed25519);
         var x25519DownloadResult = await DownloadPrivateKeyAsync(UserKeyType.X25519);

         Ed25519PrivateKey = await ed25519DownloadResult.MatchAsync(
            async downloadError => await HandleDownloadErrorAsync(downloadError, UserKeyType.Ed25519, symmetricKey, rememberUser),
            encryptedKeyInfo => HandleDownloadSuccess(encryptedKeyInfo, UserKeyType.Ed25519, symmetricKey));

         X25519PrivateKey = await x25519DownloadResult.MatchAsync(
            async downloadError => await HandleDownloadErrorAsync(downloadError, UserKeyType.X25519, symmetricKey, rememberUser),
            encryptedKeyInfo => HandleDownloadSuccess(encryptedKeyInfo, UserKeyType.X25519, symmetricKey));

         await Ed25519PrivateKey.IfSomeAsync(async x => await _userKeysRepository.StoreEd25519PrivateKeyAsync(x, rememberUser));
         await X25519PrivateKey.IfSomeAsync(async x => await _userKeysRepository.StoreX25519PrivateKeyAsync(x, rememberUser));

         return Ed25519PrivateKey.IsSome && X25519PrivateKey.IsSome;
      }

      public (PEMString PrivateKey, PEMString PublicKey) NewX25519KeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDH.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);

         return (privateKey, publicKey);
      }

      public (PEMString PrivateKey, PEMString PublicKey) NewEd25519KeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDSA.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);

         return (privateKey, publicKey);
      }

      public void Recycle()
      {
         Ed25519PrivateKey = Maybe<PEMString>.None;
         X25519PrivateKey = Maybe<PEMString>.None;
      }

      private async Task<Maybe<PEMString>> UploadNewUserKeyAsync(UserKeyType keyType, byte[] userSymmetricKey, bool rememberUser)
      {
         var (privateKey, publicKey) = keyType switch
         {
            UserKeyType.Ed25519 => NewEd25519KeyPair(),
            UserKeyType.X25519 => NewX25519KeyPair(),
            _ => throw new NotImplementedException("Unknown key type.")
         };

         var (encryptedPrivateKey, iv) = _simpleEncryptionService.Encrypt(userSymmetricKey, privateKey.Value);
         var uploadSuccess = await UploadKeyPairAsync(encryptedPrivateKey, publicKey, iv, keyType);
         if (!uploadSuccess)
         {
            return Maybe<PEMString>.None;
         }

         switch (keyType)
         {
            case UserKeyType.Ed25519:
               Ed25519PrivateKey = privateKey;
               await _userKeysRepository.StoreEd25519PrivateKeyAsync(privateKey, rememberUser);
               break;
            case UserKeyType.X25519:
               X25519PrivateKey = privateKey;
               await _userKeysRepository.StoreX25519PrivateKeyAsync(privateKey, rememberUser);
               break;
            default:
               throw new NotImplementedException("Unknown key type.");
         }

         return privateKey;
      }

      private async Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> DownloadPrivateKeyAsync(UserKeyType keyType)
      {
         return keyType switch
         {
            UserKeyType.Ed25519 => await _crypterApiService.GetUserEd25519PrivateKeyAsync(),
            UserKeyType.X25519 => await _crypterApiService.GetUserX25519PrivateKeyAsync(),
            _ => throw new NotImplementedException("Unknown key type.")
         };
      }

      private Maybe<PEMString> DecryptUserPrivateKey(Base64String encryptedPrivateKey, Base64String iv, byte[] userSymmetricKey)
      {
         byte[] decodedPrivateKey = Convert.FromBase64String(encryptedPrivateKey.Value);
         byte[] decodedIV = Convert.FromBase64String(iv.Value);
         try
         {
            string plaintextPemKey = _simpleEncryptionService.DecryptToString(userSymmetricKey, decodedIV, decodedPrivateKey);
            return PEMString.From(plaintextPemKey);
         }
         catch (Exception)
         {
            return Maybe<PEMString>.None;
         }
      }

      private static byte[] DeriveUserSymmetricKey(Username username, Password password)
      {
         return CryptoLib.UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, password);
      }

      private async Task<bool> UploadKeyPairAsync(byte[] encryptedPrivateKey, PEMString publicKey, byte[] iv, UserKeyType keyType)
      {
         var base64PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey.Value));
         var base64EncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var base64IV = Convert.ToBase64String(iv);

         var request = new UpdateKeysRequest(base64EncryptedPrivateKey, base64PublicKey, base64IV);
         var response = keyType switch
         {
            UserKeyType.Ed25519 => await _crypterApiService.InsertUserEd25519KeysAsync(request),
            UserKeyType.X25519 => await _crypterApiService.InsertUserX25519KeysAsync(request),
            _ => throw new NotImplementedException("Unknown key type.")
         };
         return response.IsRight;
      }
   }
}
