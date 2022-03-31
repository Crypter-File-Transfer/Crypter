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

using Crypter.ClientServices.DeviceStorage.Enums;
using Crypter.ClientServices.DeviceStorage.Models;
using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Events;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication.Login;
using Crypter.Contracts.Features.Authentication.Logout;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Implementations
{
   public class UserSessionService<TStorageLocation> : IUserSessionService
      where TStorageLocation : Enum
   {
      private readonly ICrypterApiService _crypterApiService;
      private readonly IUserSessionRepository _userSessionRepository;
      private readonly ITokenRepository _tokenRepository;

      private EventHandler<UserLoggedInEventArgs> _userLoggedInEventHandler;
      private EventHandler _userLoggedOutEventHandler;

      private readonly IReadOnlyDictionary<bool, TokenType> _trustDeviceRefreshTokenTypeMap;

      private readonly SemaphoreSlim _initializationMutex = new(1);
      private bool _initialized;

      public bool LoggedIn { get; protected set; }

      public Maybe<UserSession> Session { get; protected set; } = Maybe<UserSession>.None;

      public UserSessionService(
         ICrypterApiService crypterApiService,
         IUserSessionRepository userSessionRepository,
         ITokenRepository tokenRepository)
      {
         _crypterApiService = crypterApiService;
         _userSessionRepository = userSessionRepository;
         _tokenRepository = tokenRepository;

         _trustDeviceRefreshTokenTypeMap = new Dictionary<bool, TokenType>
         {
            { false, TokenType.Session },
            { true, TokenType.Device }
         };
      }

      public async Task InitializeAsync()
      {
         await _initializationMutex.WaitAsync().ConfigureAwait(false);
         try
         {
            if (!_initialized)
            {
               Session = await _userSessionRepository.GetUserSessionAsync();
               LoggedIn = Session.IsSome;
               _initialized = true;
            }
         }
         finally
         {
            _initializationMutex.Release();
         }
      }

      public async Task<bool> LoginAsync(Username username, Password password, bool rememberUser)
      {
         var eitherResponse = await SendLoginRequestAsync(username, password, _trustDeviceRefreshTokenTypeMap[rememberUser]);
         await eitherResponse.DoRightAsync(async response =>
         {
            var userPreferredStorageLocation = rememberUser
                  ? BrowserStorageLocation.LocalStorage
                  : BrowserStorageLocation.SessionStorage;

            var sessionInfo = new UserSession(response.Username, rememberUser);
            Session = sessionInfo;
            await _userSessionRepository.StoreUserSessionAsync(sessionInfo, rememberUser);

            await _tokenRepository.StoreAuthenticationTokenAsync(response.AuthenticationToken);
            await _tokenRepository.StoreRefreshTokenAsync(response.RefreshToken, _trustDeviceRefreshTokenTypeMap[rememberUser]);
         });

         LoggedIn = eitherResponse.IsRight;
         if (LoggedIn)
         {
            HandleUserLoggedInEvent(username, password, rememberUser);
         }

         return LoggedIn;
      }

      public async Task LogoutAsync()
      {
         var maybeTokenObject = await _tokenRepository.GetRefreshTokenAsync();
         await maybeTokenObject.IfSomeAsync(async tokenObject =>
         {
            var logoutRequest = new LogoutRequest(tokenObject.Token);
            await _crypterApiService.LogoutAsync(logoutRequest);
            LoggedIn = false;
            Session = Maybe<UserSession>.None;
            HandleUserLoggedOutEvent();
         });
      }

      private void HandleUserLoggedInEvent(Username username, Password password, bool rememberUser) =>
         _userLoggedInEventHandler?.Invoke(this, new UserLoggedInEventArgs(username, password, rememberUser));

      private void HandleUserLoggedOutEvent() =>
         _userLoggedOutEventHandler?.Invoke(this, EventArgs.Empty);

      public event EventHandler<UserLoggedInEventArgs> UserLoggedInEventHandler
      {
         add => _userLoggedInEventHandler = (EventHandler<UserLoggedInEventArgs>)Delegate.Combine(_userLoggedInEventHandler, value);
         remove => _userLoggedInEventHandler = (EventHandler<UserLoggedInEventArgs>)Delegate.Remove(_userLoggedInEventHandler, value);
      }

      public event EventHandler UserLoggedOutEventHandler
      {
         add => _userLoggedOutEventHandler = (EventHandler)Delegate.Combine(_userLoggedOutEventHandler, value);
         remove => _userLoggedOutEventHandler = (EventHandler)Delegate.Remove(_userLoggedOutEventHandler, value);
      }

      private async Task<Either<LoginError, LoginResponse>> SendLoginRequestAsync(Username username, Password password, TokenType refreshTokenType)
      {
         byte[] authPasswordBytes = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
         AuthenticationPassword authPassword = AuthenticationPassword.From(Convert.ToBase64String(authPasswordBytes));

         LoginRequest loginRequest = new(username, authPassword, refreshTokenType);
         return await _crypterApiService.LoginAsync(loginRequest);
      }
   }
}
