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

using Crypter.ClientServices.DeviceStorage.Models;
using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Events;
using Crypter.ClientServices.Interfaces.Repositories;
using Crypter.Common.Enums;
using Crypter.Common.Models;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/*
namespace Crypter.ClientServices.Services
{
   public class UserSessionService<TStorageLocation> : IUserSessionService, IDisposable
      where TStorageLocation : Enum
   {
      private readonly ICrypterApiService _crypterApiService;

      // Repositories
      private readonly IDeviceRepository<TStorageLocation> _deviceRepository;
      private readonly IUserSessionRepository _userSessionRepository;
      private readonly ITokenRepository _tokenRepository;

      // Events
      private EventHandler<UserSessionServiceInitializedEventArgs> _serviceInitializedEventHandler;
      private EventHandler<UserLoggedInEventArgs> _userLoggedInEventHandler;
      private EventHandler _userLoggedOutEventHandler;
      private EventHandler<UserPasswordTestSuccessEventArgs> _userPasswordTestSuccessEventHandler;

      // Configuration
      private readonly IReadOnlyDictionary<bool, TokenType> _trustDeviceRefreshTokenTypeMap;

      // Private state
      private bool _isInitialized = false;
      private readonly SemaphoreSlim _initializationMutex = new(1);

      // Public properties
      public Maybe<UserSession> Session { get; protected set; } = Maybe<UserSession>.None;

      public UserSessionService(
         ICrypterApiService crypterApiService,
         IUserSessionRepository userSessionRepository,
         ITokenRepository tokenRepository,
         IDeviceRepository<TStorageLocation> deviceRepository)
      {
         _crypterApiService = crypterApiService;
         _userSessionRepository = userSessionRepository;
         _tokenRepository = tokenRepository;
         _deviceRepository = deviceRepository;

         _trustDeviceRefreshTokenTypeMap = new Dictionary<bool, TokenType>
         {
            { false, TokenType.Session },
            { true, TokenType.Device }
         };

         _deviceRepository.InitializedEventHandler += OnDeviceRepositoryInitializedAsync;
         _crypterApiService.RefreshTokenRejectedEventHandler += OnRefreshTokenRejectedByApi;
      }

      private async Task InitializeAsync()
      {
         await _initializationMutex.WaitAsync().ConfigureAwait(false);
         try
         {
            if (!_isInitialized)
            {
               var preExistingSession = await _userSessionRepository.GetUserSessionAsync();
               await preExistingSession.IfSomeAsync(async session =>
               {
                  if (session.Schema == UserSession.LATEST_SCHEMA)
                  {
                     await _crypterApiService.RefreshAsync().DoRightAsync(async x =>
                     {
                        await _tokenRepository.StoreAuthenticationTokenAsync(x.AuthenticationToken);
                        await _tokenRepository.StoreRefreshTokenAsync(x.RefreshToken, x.RefreshTokenType);
                        Session = session;
                     });
                  }
                  else
                  {
                     await LogoutAsync();
                  }
               });

               HandleServiceInitializedEvent();
               _isInitialized = true;
            }
         }
         finally
         {
            _initializationMutex.Release();
         }
      }

      public async Task<bool> IsLoggedInAsync()
      {
         if (!_isInitialized)
         {
            await InitializeAsync();
         }

         return Session.IsSome;
      }

      public async Task<Either<LoginError, Unit>> LoginAsync(Username username, Password password, bool rememberUser)
      {
         AuthenticationPassword authPassword = _clientPbkdfService.DeriveUserAuthenticationPassword(username, password, _clientPbkdfService.CurrentPasswordVersion);
         var versionedPasswords = new List<VersionedPassword> { new VersionedPassword(authPassword, _clientPbkdfService.CurrentPasswordVersion) };
         var loginTask = from loginResponse in LoginRecursiveAsync(username, password, versionedPasswords, _trustDeviceRefreshTokenTypeMap[rememberUser])
                         from unit0 in Either<LoginError, Unit>.FromRightAsync(OnSuccessfulLoginAsync(loginResponse, rememberUser))
                         select loginResponse;

         var loginResult = await loginTask;
         loginResult.DoRight(x => HandleUserLoggedInEvent(username, password, rememberUser, x.ShowRecoveryKey));

         return loginResult.Map(_ => Unit.Default);
      }

      private Task<Either<LoginError, LoginResponse>> LoginRecursiveAsync(Username username, Password password, List<VersionedPassword> versionedPasswords, TokenType refreshTokenType)
      {
         return SendLoginRequestAsync(username, versionedPasswords, refreshTokenType)
            .MatchAsync(
            async error =>
            {
               int oldestPasswordVersionAttempted = versionedPasswords.Min(x => x.Version);
               if (error == LoginError.InvalidPasswordVersion && oldestPasswordVersionAttempted > 0)
               {
                  int nextLowestPasswordVersion = oldestPasswordVersionAttempted - 1;
                  AuthenticationPassword authPassword = _clientPbkdfService.DeriveUserAuthenticationPassword(username, password, nextLowestPasswordVersion);
                  versionedPasswords.Add(new VersionedPassword(authPassword, nextLowestPasswordVersion));
                  return await LoginRecursiveAsync(username, password, versionedPasswords, refreshTokenType);
               }
               else
               {
                  return error;
               }
            },
            response => response,
            LoginError.UnknownError);
      }

      public async Task<bool> TestPasswordAsync(Password password)
      {
         Username username = Session.Match(
            () => throw new InvalidOperationException("Invalid session"),
            x => Username.From(x.Username));

         var response = await SendTestPasswordRequestAsync(username, password);

         if (response.IsRight)
         {
            HandleTestPasswordSuccessEvent(username, password);
         }
         return response.IsRight;
      }

      public async Task<Unit> LogoutAsync()
      {
         await _crypterApiService.LogoutAsync();
         return await RecycleAsync();
      }

      private async Task<Unit> RecycleAsync()
      {
         Session = Maybe<UserSession>.None;
         await _deviceRepository.RecycleAsync();
         HandleUserLoggedOutEvent();
         return Unit.Default;
      }

      private async void OnDeviceRepositoryInitializedAsync(object sender, EventArgs _) =>
         await InitializeAsync();

      private async void OnRefreshTokenRejectedByApi(object sender, EventArgs _) =>
         await RecycleAsync();

      private void HandleServiceInitializedEvent() =>
         _serviceInitializedEventHandler?.Invoke(this, new UserSessionServiceInitializedEventArgs(Session.IsSome));

      private void HandleUserLoggedInEvent(Username username, Password password, bool rememberUser, bool showRecoveryKeyModal) =>
         _userLoggedInEventHandler?.Invoke(this, new UserLoggedInEventArgs(username, password, rememberUser, showRecoveryKeyModal));

      private void HandleUserLoggedOutEvent() =>
         _userLoggedOutEventHandler?.Invoke(this, EventArgs.Empty);

      private void HandleTestPasswordSuccessEvent(Username username, Password password) =>
         _userPasswordTestSuccessEventHandler?.Invoke(this, new UserPasswordTestSuccessEventArgs(username, password));

      public event EventHandler<UserSessionServiceInitializedEventArgs> ServiceInitializedEventHandler
      {
         add => _serviceInitializedEventHandler = (EventHandler<UserSessionServiceInitializedEventArgs>)Delegate.Combine(_serviceInitializedEventHandler, value);
         remove => _serviceInitializedEventHandler = (EventHandler<UserSessionServiceInitializedEventArgs>)Delegate.Remove(_serviceInitializedEventHandler, value);
      }

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

      public event EventHandler<UserPasswordTestSuccessEventArgs> UserPasswordTestSuccessEventHandler
      {
         add => _userPasswordTestSuccessEventHandler = (EventHandler<UserPasswordTestSuccessEventArgs>)Delegate.Combine(_userPasswordTestSuccessEventHandler, value);
         remove => _userPasswordTestSuccessEventHandler = (EventHandler<UserPasswordTestSuccessEventArgs>)Delegate.Remove(_userPasswordTestSuccessEventHandler, value);
      }

      private Task<Either<LoginError, LoginResponse>> SendLoginRequestAsync(Username username, List<VersionedPassword> versionedPasswords, TokenType refreshTokenType)
      {
         LoginRequest loginRequest = new LoginRequest(username, versionedPasswords, refreshTokenType);
         return _crypterApiService.LoginAsync(loginRequest);
      }

      private Task<Either<TestPasswordError, TestPasswordResponse>> SendTestPasswordRequestAsync(Username username, Password password)
      {
         AuthenticationPassword authPassword = _clientPbkdfService.DeriveUserAuthenticationPassword(username, password, _clientPbkdfService.CurrentPasswordVersion);
         TestPasswordRequest testRequest = new TestPasswordRequest(username, authPassword);
         return _crypterApiService.TestPasswordAsync(testRequest);
      }

      private Task<Unit> OnSuccessfulLoginAsync(LoginResponse response, bool rememberUser)
      {
         var sessionInfo = new UserSession(response.Username, rememberUser, UserSession.LATEST_SCHEMA);
         Session = sessionInfo;

         return Task.Run(async () =>
         {
            await _userSessionRepository.StoreUserSessionAsync(sessionInfo, rememberUser);
            await _tokenRepository.StoreAuthenticationTokenAsync(response.AuthenticationToken);
            await _tokenRepository.StoreRefreshTokenAsync(response.RefreshToken, _trustDeviceRefreshTokenTypeMap[rememberUser]);
            return Unit.Default;
         });
      }

      public void Dispose()
      {
         _deviceRepository.InitializedEventHandler -= OnDeviceRepositoryInitializedAsync;
         _crypterApiService.RefreshTokenRejectedEventHandler -= OnRefreshTokenRejectedByApi;
         GC.SuppressFinalize(this);
      }
   }
}
*/