/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Web.Models.LocalStorage;
using Crypter.Web.Services.API;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IAuthenticationService
   {
      Task<bool> LoginAsync(string username, string password, bool trustDevice);
      Task<bool> ReauthenticateAsync(string password);
      Task LogoutAsync();
   }

   public class AuthenticationService : IAuthenticationService
   {
      private readonly IUserApiService _userApiService;
      private readonly IUserKeysService _userKeysService;
      private readonly ILocalStorageService _localStorageService;
      private readonly ISimpleEncryptionService _simpleEncryptionService;

      public AuthenticationService(IUserApiService userApiService, IUserKeysService userKeysService, ILocalStorageService localStorageService, ISimpleEncryptionService simpleEncryptionService)
      {
         _userApiService = userApiService;
         _userKeysService = userKeysService;
         _localStorageService = localStorageService;
         _simpleEncryptionService = simpleEncryptionService;
      }

      public async Task<bool> LoginAsync(string username, string password, bool trustDevice)
      {
         var (authSuccess, authResponse) = await SendAuthenticationRequestAsync(username, password, trustDevice);
         if (!authSuccess)
         {
            return false;
         }

         var userPreferredStorageLocation = trustDevice
            ? StorageLocation.LocalStorage
            : StorageLocation.SessionStorage;

         var userSymmetricKey = _userKeysService.GetUserSymmetricKey(username, password);

         await CacheSessionInfoAsync(authResponse, username, userSymmetricKey, userPreferredStorageLocation);
         await HandleUserKeys(authResponse, userSymmetricKey, userPreferredStorageLocation);
         return true;
      }

      public async Task<bool> ReauthenticateAsync(string password)
      {
         var userSessionInfo = await _localStorageService.GetItemAsync<UserSession>(StoredObjectType.UserSession);
         var userSymmetricKey = _userKeysService.GetUserSymmetricKey(userSessionInfo.Username, password);
         var userEncryptedX25519 = await _localStorageService.GetItemAsync<EncryptedPrivateKey>(StoredObjectType.EncryptedX25519PrivateKey);
         var userEncryptedEd25519 = await _localStorageService.GetItemAsync<EncryptedPrivateKey>(StoredObjectType.EncryptedEd25519PrivateKey);

         var (decryptTokenSuccess, token) = DecryptLocalAuthToken(userSymmetricKey, userSessionInfo);
         var (decryptX25519Success, x25519) = DecryptLocalPrivateKey(userSymmetricKey, userEncryptedX25519);
         var (decryptEd25519Success, ed25519) = DecryptLocalPrivateKey(userSymmetricKey, userEncryptedEd25519);

         if (decryptTokenSuccess && decryptX25519Success && decryptEd25519Success)
         {
            await _localStorageService.SetItemAsync(StoredObjectType.AuthToken, token, StorageLocation.InMemory);
            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextX25519PrivateKey, x25519, StorageLocation.InMemory);
            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextEd25519PrivateKey, ed25519, StorageLocation.InMemory);

            // TODO - Test the token by hitting the API
            // If the API rejects the token, perform a full-fledged login since we already have the users credentials

            return true;
         }
         return false;
      }

      public async Task LogoutAsync()
      {
         await _localStorageService.DisposeAsync();
      }

      private async Task CacheSessionInfoAsync(UserAuthenticateResponse authResponse, string username, byte[] userSymmetricKey, StorageLocation storageLocation)
      {
         var (encryptedToken, tokenIV) = _simpleEncryptionService.Encrypt(userSymmetricKey, authResponse.Token);
         var base64EncryptedToken = Convert.ToBase64String(encryptedToken);
         var base64TokenIV = Convert.ToBase64String(tokenIV);

         var sessionInfo = new UserSession(authResponse.Id, username, base64EncryptedToken, base64TokenIV);
         await _localStorageService.SetItemAsync(StoredObjectType.UserSession, sessionInfo, storageLocation);

         // The plaintext JWT should only be stored InMemory
         await _localStorageService.SetItemAsync(StoredObjectType.AuthToken, authResponse.Token, StorageLocation.InMemory);
      }

      private async Task<(bool success, UserAuthenticateResponse response)> SendAuthenticationRequestAsync(string username, string password, bool requestRefreshToken)
      {
         byte[] digestedPassword = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var loginRequest = new AuthenticateUserRequest(username, digestedPasswordBase64, requestRefreshToken);
         var (httpStatus, authResponse) = await _userApiService.AuthenticateUserAsync(loginRequest);
         return (httpStatus == HttpStatusCode.OK, authResponse);
      }

      private async Task HandleUserKeys(UserAuthenticateResponse authResponse, byte[] userSymmetricKey, StorageLocation storageLocation)
      {
         // Handle X25519
         if (string.IsNullOrEmpty(authResponse.EncryptedX25519PrivateKey))
         {
            var (privateKey, publicKey) = _userKeysService.NewX25519KeyPair();
            var (encryptedPrivateKey, iv) = _simpleEncryptionService.Encrypt(userSymmetricKey, privateKey);
            await UploadKeyPairAsync(encryptedPrivateKey, publicKey, iv, PublicKeyType.X25519);

            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextX25519PrivateKey, privateKey, StorageLocation.InMemory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, StoredObjectType.EncryptedX25519PrivateKey, storageLocation);
         }
         else
         {
            var iv = Convert.FromBase64String(authResponse.X25519IV);
            var encryptedPrivateKey = Convert.FromBase64String(authResponse.EncryptedX25519PrivateKey);
            var plaintextPrivateKey = _simpleEncryptionService.DecryptToString(userSymmetricKey, iv, encryptedPrivateKey);

            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextX25519PrivateKey, plaintextPrivateKey, StorageLocation.InMemory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, StoredObjectType.EncryptedX25519PrivateKey, storageLocation);
         }

         // Handle Ed25519
         if (string.IsNullOrEmpty(authResponse.EncryptedEd25519PrivateKey))
         {
            var (privateKey, publicKey) = _userKeysService.NewEd25519KeyPair();
            var (encryptedPrivateKey, iv) = _simpleEncryptionService.Encrypt(userSymmetricKey, privateKey);
            await UploadKeyPairAsync(encryptedPrivateKey, publicKey, iv, PublicKeyType.Ed25519);

            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextEd25519PrivateKey, privateKey, StorageLocation.InMemory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, StoredObjectType.EncryptedEd25519PrivateKey, storageLocation);
         }
         else
         {
            var iv = Convert.FromBase64String(authResponse.Ed25519IV);
            var encryptedPrivateKey = Convert.FromBase64String(authResponse.EncryptedEd25519PrivateKey);
            var plaintextPrivateKey = _simpleEncryptionService.DecryptToString(userSymmetricKey, iv, encryptedPrivateKey);

            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextEd25519PrivateKey, plaintextPrivateKey, StorageLocation.InMemory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, StoredObjectType.EncryptedEd25519PrivateKey, storageLocation);
         }
      }

      private async Task UploadKeyPairAsync(byte[] encryptedPrivateKey, string publicKey, byte[] iv, PublicKeyType keyType)
      {
         var base64PublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
         var base64EncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var base64IV = Convert.ToBase64String(iv);

         var request = new UpdateKeysRequest(base64EncryptedPrivateKey, base64PublicKey, base64IV);
         switch (keyType)
         {
            case PublicKeyType.X25519:
               await _userApiService.InsertUserX25519KeysAsync(request);
               break;
            case PublicKeyType.Ed25519:
               await _userApiService.InsertUserEd25519KeysAsync(request);
               break;
            default:
               throw new ArgumentException("Invalid key type");
         }
      }

      private async Task CacheEncryptedPrivateKey(byte[] encryptedPrivateKey, byte[] iv, StoredObjectType objectType, StorageLocation storageLocation)
      {
         var base64EncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var base64IV = Convert.ToBase64String(iv);
         var storageModel = new EncryptedPrivateKey(base64EncryptedPrivateKey, base64IV);
         await _localStorageService.SetItemAsync(objectType, storageModel, storageLocation);
      }

      private (bool success, string token) DecryptLocalAuthToken(byte[] userSymmetricKey, UserSession sessionInfo)
      {
         var encryptedToken = Convert.FromBase64String(sessionInfo.EncryptedAuthToken);
         var iv = Convert.FromBase64String(sessionInfo.AuthTokenIV);

         try
         {
            var plaintextAuthToken = _simpleEncryptionService.DecryptToString(userSymmetricKey, iv, encryptedToken);
            return (true, plaintextAuthToken);
         }
         catch (Exception)
         {
            return (false, null);
         }
      }

      private (bool success, string privateKey) DecryptLocalPrivateKey(byte[] userSymmetricKey, EncryptedPrivateKey encryptionInfo)
      {
         var encryptedKey = Convert.FromBase64String(encryptionInfo.Key);
         var iv = Convert.FromBase64String(encryptionInfo.IV);

         try
         {
            var plaintextKey = _simpleEncryptionService.DecryptToString(userSymmetricKey, iv, encryptedKey);
            System.Console.WriteLine(plaintextKey);
            return (true, plaintextKey);
         }
         catch (Exception)
         {
            return (false, null);
         }
      }
   }
}
