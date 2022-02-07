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

using Crypter.Common.FunctionalTypes;
using Crypter.Contracts.Common;
using Crypter.Contracts.Common.Enum;
using Crypter.Contracts.Features.Authentication.Login;
using Crypter.Contracts.Features.Authentication.Logout;
using Crypter.Contracts.Features.User.UpdateKeys;
using Crypter.CryptoLib.Services;
using Crypter.Web.Models.LocalStorage;
using Crypter.Web.Services.API;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IAuthenticationService
   {
      Task<bool> LoginAsync(string username, string password, bool trustDevice);
      Task<bool> UnlockSession(string password);
      Task<bool> TryRefreshingTokenAsync();
      event EventHandler<UserSessionStateChangedEventArgs> UserSessionStateChanged;
      Task LogoutAsync();
   }

   public class AuthenticationService : IAuthenticationService
   {
      private readonly IUserApiService _userApiService;
      private readonly IUserKeysService _userKeysService;
      private readonly IAuthenticationApiService _authenticationApiService;
      private readonly ILocalStorageService _localStorageService;
      private readonly ISimpleEncryptionService _simpleEncryptionService;

      private EventHandler<UserSessionStateChangedEventArgs> _userSessionStateChanged;

      public AuthenticationService(IUserApiService userApiService, IUserKeysService userKeysService, IAuthenticationApiService authenticationApiService, ILocalStorageService localStorageService, ISimpleEncryptionService simpleEncryptionService)
      {
         _userApiService = userApiService;
         _userKeysService = userKeysService;
         _authenticationApiService = authenticationApiService;
         _localStorageService = localStorageService;
         _simpleEncryptionService = simpleEncryptionService;
      }

      public async Task<bool> LoginAsync(string username, string password, bool trustDevice)
      {
         var refreshTokenType = trustDevice
            ? TokenType.Refresh
            : TokenType.Session;

         var maybeLogin = await SendLoginRequestAsync(username, password, refreshTokenType);
         return await maybeLogin.MatchAsync(
            left => Task.FromResult(false),
            async right =>
            {
               var userPreferredStorageLocation = trustDevice
                  ? StorageLocation.LocalStorage
                  : StorageLocation.SessionStorage;

               var userSymmetricKey = _userKeysService.GetUserSymmetricKey(username, password);

               await CacheSessionInfoAsync(right, username, userPreferredStorageLocation);
               await HandleUserKeys(right, userSymmetricKey, userPreferredStorageLocation);
               NotifyUserSessionStateChanged(true, right.Id, username);
               return true;
            }
         );
      }

      public async Task<bool> UnlockSession(string password)
      {
         var userSessionInfo = await _localStorageService.GetItemAsync<UserSession>(StoredObjectType.UserSession);
         var userSymmetricKey = _userKeysService.GetUserSymmetricKey(userSessionInfo.Username, password);

         if (!await TryRefreshingTokenAsync())
         {
            return false;
         }

         var userEncryptedX25519 = await _localStorageService.GetItemAsync<EncryptedPrivateKey>(StoredObjectType.EncryptedX25519PrivateKey);
         var userEncryptedEd25519 = await _localStorageService.GetItemAsync<EncryptedPrivateKey>(StoredObjectType.EncryptedEd25519PrivateKey);

         var (decryptX25519Success, x25519) = DecryptLocalPrivateKey(userSymmetricKey, userEncryptedX25519);
         var (decryptEd25519Success, ed25519) = DecryptLocalPrivateKey(userSymmetricKey, userEncryptedEd25519);

         if (decryptX25519Success && decryptEd25519Success)
         {
            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextX25519PrivateKey, x25519, StorageLocation.InMemory);
            await _localStorageService.SetItemAsync(StoredObjectType.PlaintextEd25519PrivateKey, ed25519, StorageLocation.InMemory);
            return true;
         }
         return false;
      }

      public async Task<bool> TryRefreshingTokenAsync()
      {
         var sessionInfo = await _localStorageService.GetItemAsync<UserSession>(StoredObjectType.UserSession);
         var maybeRefresh = await _authenticationApiService.RefreshAsync();
         return await maybeRefresh.MatchAsync(
            left => Task.FromResult(false),
            async right =>
            {
               sessionInfo.RefreshToken = right.RefreshToken;
               var sessionLocation = _localStorageService.GetItemLocation(StoredObjectType.UserSession);
               await _localStorageService.SetItemAsync(StoredObjectType.UserSession, sessionInfo, sessionLocation);
               await _localStorageService.SetItemAsync(StoredObjectType.AuthenticationToken, right.AuthenticationToken, StorageLocation.InMemory);
               return true;
            }
         );
      }

      public event EventHandler<UserSessionStateChangedEventArgs> UserSessionStateChanged
      {
         add
         {
            _userSessionStateChanged = (EventHandler<UserSessionStateChangedEventArgs>)Delegate.Combine(_userSessionStateChanged, value);
         }
         remove
         {
            _userSessionStateChanged = (EventHandler<UserSessionStateChangedEventArgs>)Delegate.Remove(_userSessionStateChanged, value);
         }
      }

      public async Task LogoutAsync()
      {
         var userSessionInfo = await _localStorageService.GetItemAsync<UserSession>(StoredObjectType.UserSession);
         var logoutRequest = new LogoutRequest(userSessionInfo.RefreshToken);
         await _authenticationApiService.LogoutAsync(logoutRequest);
         NotifyUserSessionStateChanged(false);
         await _localStorageService.DisposeAsync();
      }

      private async Task CacheSessionInfoAsync(LoginResponse authResponse, string username, StorageLocation storageLocation)
      {
         var sessionInfo = new UserSession(authResponse.Id, username, authResponse.RefreshToken);
         await _localStorageService.SetItemAsync(StoredObjectType.UserSession, sessionInfo, storageLocation);

         // Plaintext authentication token should be stored in-memory; not in browser storage
         await _localStorageService.SetItemAsync(StoredObjectType.AuthenticationToken, authResponse.AuthenticationToken, StorageLocation.InMemory);
      }

      private async Task<Either<ErrorResponse, LoginResponse>> SendLoginRequestAsync(string username, string password, TokenType refreshTokenType)
      {
         byte[] digestedPassword = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var loginRequest = new LoginRequest(username, digestedPasswordBase64, refreshTokenType);
         return await _authenticationApiService.LoginAsync(loginRequest);
      }

      private async Task HandleUserKeys(LoginResponse authResponse, byte[] userSymmetricKey, StorageLocation storageLocation)
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

      private (bool success, string privateKey) DecryptLocalPrivateKey(byte[] userSymmetricKey, EncryptedPrivateKey encryptionInfo)
      {
         var encryptedKey = Convert.FromBase64String(encryptionInfo.Key);
         var iv = Convert.FromBase64String(encryptionInfo.IV);

         try
         {
            var plaintextKey = _simpleEncryptionService.DecryptToString(userSymmetricKey, iv, encryptedKey);
            return (true, plaintextKey);
         }
         catch (Exception)
         {
            return (false, null);
         }
      }

      private void NotifyUserSessionStateChanged(bool loggedIn, Guid userId = default, string username = default)
      {
         _userSessionStateChanged?.Invoke(this, new UserSessionStateChangedEventArgs(loggedIn, userId, username));
      }
   }

   public class UserSessionStateChangedEventArgs : EventArgs
   {
      public bool LoggedIn { get; set; }
      public Guid UserId { get; set; }
      public string Username { get; }

      public UserSessionStateChangedEventArgs(bool loggedIn, Guid userId, string username)
      {
         LoggedIn = loggedIn;
         UserId = userId;
         Username = username;
      }
   }
}
