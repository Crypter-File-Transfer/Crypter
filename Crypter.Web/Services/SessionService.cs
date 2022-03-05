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
using Crypter.Contracts.Features.Authentication.Login;
using Crypter.Contracts.Features.Authentication.Logout;
using Crypter.Contracts.Features.User.UpdateKeys;
using Crypter.CryptoLib.Services;
using Crypter.Web.Models.LocalStorage;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface ISessionService
   {
      bool IsLoggedIn { get; }
      Task<bool> LoginAsync(string username, string password, bool trustDevice);
      Task<bool> UnlockSession(string password);
      Task<bool> TryRefreshingTokenAsync();
      Task<UserSession> GetCurrentUserSessionAsync();
      event EventHandler<UserSessionStateChangedEventArgs> UserSessionStateChanged;
      Task LogoutAsync();
   }

   public class SessionService : ISessionService
   {
      private readonly ICrypterApiService _crypterApiService;
      private readonly IUserKeysService _userKeysService;
      private readonly IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation> _browserStorageService;
      private readonly ISimpleEncryptionService _simpleEncryptionService;
      private readonly IUserContactsService _userContactsService;

      private EventHandler<UserSessionStateChangedEventArgs> _userSessionStateChanged;

      public bool IsLoggedIn { get; private set; } = false;

      public SessionService(
         ICrypterApiService crypterApiService,
         IUserKeysService userKeysService,
         IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation> browserStorageService,
         ISimpleEncryptionService simpleEncryptionService,
         IUserContactsService userContactsService)
      {
         _crypterApiService = crypterApiService;
         _userKeysService = userKeysService;
         _browserStorageService = browserStorageService;
         _simpleEncryptionService = simpleEncryptionService;
         _userContactsService = userContactsService;
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

      public async Task<bool> LoginAsync(string username, string password, bool trustDevice)
      {
         var refreshTokenType = trustDevice
            ? TokenType.Refresh
            : TokenType.Session;

         var response = await SendLoginRequestAsync(username, password, refreshTokenType);
         await response.DoRightAsync(async x =>
         {
            var userPreferredStorageLocation = trustDevice
                  ? BrowserStorageLocation.LocalStorage
                  : BrowserStorageLocation.SessionStorage;

            var userSymmetricKey = _userKeysService.GetUserSymmetricKey(username, password);

            await CacheSessionInfoAsync(x, username, userPreferredStorageLocation);
            await HandleUserKeys(x, userSymmetricKey, userPreferredStorageLocation);
            await _userContactsService.InitializeAsync();
            NotifyUserSessionStateChanged(true, x.Id, username);
         });

         IsLoggedIn = response.IsRight;
         return IsLoggedIn;
      }

      public async Task<bool> UnlockSession(string password)
      {
         var userSessionInfo = await _browserStorageService.GetItemAsync<UserSession>(BrowserStoredObjectType.UserSession);
         var userSymmetricKey = _userKeysService.GetUserSymmetricKey(userSessionInfo.Username, password);

         if (!await TryRefreshingTokenAsync())
         {
            return false;
         }

         var userEncryptedX25519 = await _browserStorageService.GetItemAsync<EncryptedPrivateKey>(BrowserStoredObjectType.EncryptedX25519PrivateKey);
         var userEncryptedEd25519 = await _browserStorageService.GetItemAsync<EncryptedPrivateKey>(BrowserStoredObjectType.EncryptedEd25519PrivateKey);

         var (decryptX25519Success, x25519) = DecryptLocalPrivateKey(userSymmetricKey, userEncryptedX25519);
         var (decryptEd25519Success, ed25519) = DecryptLocalPrivateKey(userSymmetricKey, userEncryptedEd25519);

         IsLoggedIn = decryptX25519Success && decryptEd25519Success;
         if (IsLoggedIn)
         {
            await _browserStorageService.SetItemAsync(BrowserStoredObjectType.PlaintextX25519PrivateKey, x25519, BrowserStorageLocation.Memory);
            await _browserStorageService.SetItemAsync(BrowserStoredObjectType.PlaintextEd25519PrivateKey, ed25519, BrowserStorageLocation.Memory);
            await _userContactsService.InitializeAsync();
         }
         return IsLoggedIn;
      }

      public async Task<bool> TryRefreshingTokenAsync()
      {
         var response = await _crypterApiService.RefreshAsync();
         await response.DoRightAsync(async x =>
         {
            await _browserStorageService.SetItemAsync(BrowserStoredObjectType.AuthenticationToken, x.AuthenticationToken, BrowserStorageLocation.Memory);
            await _browserStorageService.ReplaceItemAsync(BrowserStoredObjectType.RefreshToken, x.RefreshToken);
         });

         return response.IsRight;
      }

      public async Task<UserSession> GetCurrentUserSessionAsync()
      {
         return await _browserStorageService.GetItemAsync<UserSession>(BrowserStoredObjectType.UserSession);
      }

      public async Task LogoutAsync()
      {
         string refreshToken = await _browserStorageService.GetItemAsync<string>(BrowserStoredObjectType.RefreshToken);
         var logoutRequest = new LogoutRequest(refreshToken);
         _userContactsService.Dispose();
         await _crypterApiService.LogoutAsync(logoutRequest);
         IsLoggedIn = false;
         NotifyUserSessionStateChanged(false);
         await _browserStorageService.DisposeAsync();
      }

      private async Task CacheSessionInfoAsync(LoginResponse authResponse, string username, BrowserStorageLocation storageLocation)
      {
         // Store session information and refresh token in the same location
         var sessionInfo = new UserSession(authResponse.Id, username);
         await _browserStorageService.SetItemAsync(BrowserStoredObjectType.UserSession, sessionInfo, storageLocation);
         await _browserStorageService.SetItemAsync(BrowserStoredObjectType.RefreshToken, authResponse.RefreshToken, storageLocation);

         // Plaintext authentication token should only be stored in memory
         await _browserStorageService.SetItemAsync(BrowserStoredObjectType.AuthenticationToken, authResponse.AuthenticationToken, BrowserStorageLocation.Memory);
      }

      private async Task<Either<LoginError, LoginResponse>> SendLoginRequestAsync(string username, string password, TokenType refreshTokenType)
      {
         byte[] digestedPassword = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var loginRequest = new LoginRequest(username, digestedPasswordBase64, refreshTokenType);
         return await _crypterApiService.LoginAsync(loginRequest);
      }

      private async Task HandleUserKeys(LoginResponse authResponse, byte[] userSymmetricKey, BrowserStorageLocation storageLocation)
      {
         // Handle X25519
         if (string.IsNullOrEmpty(authResponse.EncryptedX25519PrivateKey))
         {
            var (privateKey, publicKey) = _userKeysService.NewX25519KeyPair();
            var (encryptedPrivateKey, iv) = _simpleEncryptionService.Encrypt(userSymmetricKey, privateKey);
            await UploadKeyPairAsync(encryptedPrivateKey, publicKey, iv, PublicKeyType.X25519);

            await _browserStorageService.SetItemAsync(BrowserStoredObjectType.PlaintextX25519PrivateKey, privateKey, BrowserStorageLocation.Memory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, BrowserStoredObjectType.EncryptedX25519PrivateKey, storageLocation);
         }
         else
         {
            var iv = Convert.FromBase64String(authResponse.X25519IV);
            var encryptedPrivateKey = Convert.FromBase64String(authResponse.EncryptedX25519PrivateKey);
            var plaintextPrivateKey = _simpleEncryptionService.DecryptToString(userSymmetricKey, iv, encryptedPrivateKey);

            await _browserStorageService.SetItemAsync(BrowserStoredObjectType.PlaintextX25519PrivateKey, plaintextPrivateKey, BrowserStorageLocation.Memory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, BrowserStoredObjectType.EncryptedX25519PrivateKey, storageLocation);
         }

         // Handle Ed25519
         if (string.IsNullOrEmpty(authResponse.EncryptedEd25519PrivateKey))
         {
            var (privateKey, publicKey) = _userKeysService.NewEd25519KeyPair();
            var (encryptedPrivateKey, iv) = _simpleEncryptionService.Encrypt(userSymmetricKey, privateKey);
            await UploadKeyPairAsync(encryptedPrivateKey, publicKey, iv, PublicKeyType.Ed25519);

            await _browserStorageService.SetItemAsync(BrowserStoredObjectType.PlaintextEd25519PrivateKey, privateKey, BrowserStorageLocation.Memory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, BrowserStoredObjectType.EncryptedEd25519PrivateKey, storageLocation);
         }
         else
         {
            var iv = Convert.FromBase64String(authResponse.Ed25519IV);
            var encryptedPrivateKey = Convert.FromBase64String(authResponse.EncryptedEd25519PrivateKey);
            var plaintextPrivateKey = _simpleEncryptionService.DecryptToString(userSymmetricKey, iv, encryptedPrivateKey);

            await _browserStorageService.SetItemAsync(BrowserStoredObjectType.PlaintextEd25519PrivateKey, plaintextPrivateKey, BrowserStorageLocation.Memory);
            await CacheEncryptedPrivateKey(encryptedPrivateKey, iv, BrowserStoredObjectType.EncryptedEd25519PrivateKey, storageLocation);
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
               await _crypterApiService.InsertUserX25519KeysAsync(request);
               break;
            case PublicKeyType.Ed25519:
               await _crypterApiService.InsertUserEd25519KeysAsync(request);
               break;
            default:
               throw new ArgumentException("Invalid key type");
         }
      }

      private async Task CacheEncryptedPrivateKey(byte[] encryptedPrivateKey, byte[] iv, BrowserStoredObjectType objectType, BrowserStorageLocation storageLocation)
      {
         var base64EncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var base64IV = Convert.ToBase64String(iv);
         var storageModel = new EncryptedPrivateKey(base64EncryptedPrivateKey, base64IV);
         await _browserStorageService.SetItemAsync(objectType, storageModel, storageLocation);
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
