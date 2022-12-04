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
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Keys;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.KeyExchange;
using Crypter.Crypto.Common.PasswordHash;
using System;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class UserKeysService : IUserKeysService, IDisposable
   {
      private readonly ICrypterApiService _crypterApiService;
      private readonly IUserKeysRepository _userKeysRepository;
      private readonly IUserSessionService _userSessionService;
      private readonly ICryptoProvider _cryptoProvider;

      public Maybe<byte[]> PrivateKey { get; protected set; }

      public UserKeysService(ICrypterApiService crypterApiService, IUserKeysRepository userKeysRepository, IUserSessionService userSessionService, ICryptoProvider cryptoProvider)
      {
         _crypterApiService = crypterApiService;
         _userKeysRepository = userKeysRepository;
         _userSessionService = userSessionService;
         _cryptoProvider = cryptoProvider;

         _userSessionService.ServiceInitializedEventHandler += OnUserSessionServiceInitialized;
         _userSessionService.UserLoggedInEventHandler += OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler += OnUserLoggedOut;
      }

      private async Task PrepareUserKeysOnUserLoginAsync(Username username, Password password, bool rememberUser)
      {
         Task<Maybe<byte[]>> HandleDownloadErrorAsync(GetPrivateKeyError error, byte[] userSymmetricKey, bool rememberUser)
         {
            return error switch
            {
               GetPrivateKeyError.NotFound => UploadNewUserKeyAsync(userSymmetricKey, rememberUser),
               _ => Maybe<byte[]>.None.AsTask()
            };
         }

         Maybe<byte[]> HandleDownloadSuccess(GetPrivateKeyResponse encryptedKeyInfo, byte[] userSymmetricKey)
         {
            return _cryptoProvider.Encryption.Decrypt(userSymmetricKey, encryptedKeyInfo.Nonce, encryptedKeyInfo.EncryptedKey);
         }

         Maybe<byte[]> symmetricKeyResult = DeriveUserSymmetricKey(username, password);
         await symmetricKeyResult.IfSomeAsync(async symmetricKey =>
         {
            Either<GetPrivateKeyError, GetPrivateKeyResponse> privateKeyDownloadResult = await _crypterApiService.GetPrivateKeyAsync();

            PrivateKey = await privateKeyDownloadResult.MatchAsync(
               async downloadError => await HandleDownloadErrorAsync(downloadError, symmetricKey, rememberUser),
               encryptedKeyInfo => HandleDownloadSuccess(encryptedKeyInfo, symmetricKey),
               Maybe<byte[]>.None);

            await PrivateKey.IfSomeAsync(async x => await _userKeysRepository.StorePrivateKeyAsync(x, rememberUser));
         });

         symmetricKeyResult.IfNone(() => throw new Exception("Unable to derive symmetric key for user"));
      }

      private async Task<Maybe<byte[]>> UploadNewUserKeyAsync(byte[] userSymmetricKey, bool rememberUser)
      {
         X25519KeyPair keyPair = _cryptoProvider.KeyExchange.GenerateKeyPair();
         Console.WriteLine($"Generated a new private key of length: {keyPair.PrivateKey.Length}");
         byte[] nonce = _cryptoProvider.Random.GenerateRandomBytes(checked((int)_cryptoProvider.Encryption.NonceSize));
         byte[] encryptedPrivateKey = _cryptoProvider.Encryption.Encrypt(userSymmetricKey, nonce, keyPair.PrivateKey);

         var uploadResult = from uploadResponse in UploadKeyPairAsync(encryptedPrivateKey, keyPair.PublicKey, nonce)
                            from unit0 in Either<InsertKeyPairError, Unit>.FromRightAsync(StorePrivateKeyAsync(keyPair.PrivateKey, rememberUser))
                            select uploadResponse;

         return await uploadResult
            .ToMaybeTask()
            .BindAsync(x => Maybe<byte[]>.From(keyPair.PrivateKey).AsTask());
      }

      private async Task<Unit> StorePrivateKeyAsync(byte[] privateKey, bool rememberUser)
      {
         PrivateKey = privateKey;
         await _userKeysRepository.StorePrivateKeyAsync(privateKey, rememberUser);
         return Unit.Default;
      }

      private Maybe<byte[]> DeriveUserSymmetricKey(Username username, Password password)
      {
         uint hashKeySize = _cryptoProvider.GenericHash.KeySize;
         byte[] hashedUsername = _cryptoProvider.GenericHash.GenerateHash(hashKeySize, username.Value.ToLower());

         uint saltSize = _cryptoProvider.PasswordHash.SaltSize;
         byte[] salt = _cryptoProvider.GenericHash.GenerateHash(saltSize, password.Value, hashedUsername);

         uint keySize = _cryptoProvider.Encryption.KeySize;
         return _cryptoProvider.PasswordHash.GenerateKey(password.Value, salt, keySize, OpsLimit.Sensitive, MemLimit.Sensitive)
            .ToMaybe();
      }

      private Task<Either<InsertKeyPairError, InsertKeyPairResponse>> UploadKeyPairAsync(byte[] encryptedPrivateKey, byte[] publicKey, byte[] nonce)
      {
         InsertKeyPairRequest request = new InsertKeyPairRequest(encryptedPrivateKey, publicKey, nonce);
         return _crypterApiService.InsertKeyPairAsync(request);
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
         await PrepareUserKeysOnUserLoginAsync(args.Username, args.Password, args.RememberUser);
      }

      private void OnUserLoggedOut(object sender, EventArgs _)
      {
         PrivateKey = Maybe<byte[]>.None;
      }

      public void Dispose()
      {
         _userSessionService.UserLoggedInEventHandler -= OnUserLoggedIn;
         _userSessionService.UserLoggedOutEventHandler -= OnUserLoggedOut;
         GC.SuppressFinalize(this);
      }
   }
}
