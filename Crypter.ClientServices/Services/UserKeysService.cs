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
         Task<Maybe<PEMString>> HandleDownloadErrorAsync(GetPrivateKeyError error, UserKeyType keyType, byte[] userSymmetricKey, bool rememberUser)
         {
            return error switch
            {
               GetPrivateKeyError.NotFound => UploadNewUserKeyAsync(keyType, userSymmetricKey, rememberUser),
               _ => Maybe<PEMString>.None.AsTask()
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
            encryptedKeyInfo => HandleDownloadSuccess(encryptedKeyInfo, UserKeyType.Ed25519, symmetricKey),
            Maybe<PEMString>.None);

         X25519PrivateKey = await x25519DownloadResult.MatchAsync(
            async downloadError => await HandleDownloadErrorAsync(downloadError, UserKeyType.X25519, symmetricKey, rememberUser),
            encryptedKeyInfo => HandleDownloadSuccess(encryptedKeyInfo, UserKeyType.X25519, symmetricKey),
            Maybe<PEMString>.None);

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

      private async Task<Maybe<PEMString>> UploadNewUserKeyAsync(UserKeyType keyType, byte[] userSymmetricKey, bool rememberUser)
      {
         var (privateKey, publicKey) = keyType switch
         {
            UserKeyType.Ed25519 => CreateEd25519KeyPair(),
            UserKeyType.X25519 => CreateX25519KeyPair(),
            _ => throw new NotImplementedException("Unknown key type.")
         };

         var (encryptedPrivateKey, iv) = _simpleEncryptionService.Encrypt(userSymmetricKey, privateKey.Value);

         var uploadResult = from uploadResponse in UploadKeyPairAsync(encryptedPrivateKey, publicKey, iv, keyType)
                            from unit0 in Either<InsertKeyPairError, Unit>.FromRightAsync(StorePrivateKeyAsync(keyType, privateKey, rememberUser))
                            select uploadResponse;

         return await uploadResult
            .ToMaybeTask()
            .BindAsync(x => Maybe<PEMString>.From(privateKey).AsTask());
      }

      private async Task<Unit> StorePrivateKeyAsync(UserKeyType keyType, PEMString privateKey, bool rememberUser)
      {
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
         return Unit.Default;
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
