﻿/*
 * Copyright (C) 2025 Crypter File Transfer
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Core.Features.UserAuthentication.Events;
using Crypter.Core.Identity;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.UserAuthentication.Commands;

public sealed record UserLoginCommand(LoginRequest Request, string DeviceDescription)
    : IEitherRequest<LoginError, LoginResponse>;

internal sealed class UserLoginCommandHandler
    : IEitherRequestHandler<UserLoginCommand, LoginError, LoginResponse>
{
    private readonly DataContext _dataContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPublisher _publisher;
    private readonly ITokenService _tokenService;
    
    private readonly short _clientPasswordVersion;
    private const int MaximumFailedLoginAttempts = 3;
    private readonly Dictionary<TokenType, Func<Guid, RefreshTokenData>> _refreshTokenProviderMap;

    private UserEntity? _foundUserEntity;

    public UserLoginCommandHandler(
        DataContext dataContext,
        IOptions<ServerPasswordSettings> passwordSettings,
        IPasswordHashService passwordHashService,
        IPublisher publisher,
        ITokenService tokenService)
    {
        _dataContext = dataContext;
        _passwordHashService = passwordHashService;
        _publisher = publisher;
        _tokenService = tokenService;
        
        _clientPasswordVersion = passwordSettings.Value.ClientVersion;
        _refreshTokenProviderMap = new Dictionary<TokenType, Func<Guid, RefreshTokenData>>
        {
            { TokenType.Session, _tokenService.NewSessionToken },
            { TokenType.Device, _tokenService.NewDeviceToken }
        };
    }
    
    public async Task<Either<LoginError, LoginResponse>> Handle(UserLoginCommand request, CancellationToken cancellationToken)
    {
        return await ValidateLoginRequest(request.Request)
            .BindAsync(async validLoginRequest => await (
                from foundUser in GetUserAsync(validLoginRequest)
                from passwordVerificationSuccess in VerifyAndUpgradePassword(validLoginRequest, foundUser).AsTask()
                from loginResponse in CreateLoginResponseAsync(foundUser, validLoginRequest.RefreshTokenType, request.DeviceDescription)
                select loginResponse)
            )
            .DoRightAsync(async _ =>
            {
                SuccessfulUserLoginEvent successfulUserLoginEvent = new SuccessfulUserLoginEvent(_foundUserEntity!.Id, request.DeviceDescription, _foundUserEntity.LastLogin);
                await _publisher.Publish(successfulUserLoginEvent, CancellationToken.None);
            })
            .DoLeftOrNeitherAsync(async error =>
            {
                FailedUserLoginEvent failedUserLoginEvent = new FailedUserLoginEvent(request.Request.Username, error, request.DeviceDescription, DateTimeOffset.UtcNow);
                await _publisher.Publish(failedUserLoginEvent, CancellationToken.None);

                if (error == LoginError.InvalidPassword)
                {
                    IncorrectPasswordProvidedEvent incorrectPasswordProvidedEvent = new IncorrectPasswordProvidedEvent(_foundUserEntity!.Id);
                    await _publisher.Publish(incorrectPasswordProvidedEvent, CancellationToken.None);
                }
            },
            async () =>
            {
                FailedUserLoginEvent failedUserLoginEvent = new FailedUserLoginEvent(request.Request.Username, LoginError.UnknownError, request.DeviceDescription, DateTimeOffset.UtcNow);
                await _publisher.Publish(failedUserLoginEvent, CancellationToken.None);
            });
    }
    
    private readonly struct ValidLoginRequest(Username username, IDictionary<short, byte[]> versionedPasswords, TokenType refreshTokenType)
    {
        public Username Username { get; } = username;
        public IDictionary<short, byte[]> VersionedPasswords { get; } = versionedPasswords;
        public TokenType RefreshTokenType { get; } = refreshTokenType;
    }
    
    private Either<LoginError, ValidLoginRequest> ValidateLoginRequest(LoginRequest request)
    {
        if (!Username.TryFrom(request.Username, out Username? validUsername))
        {
            return LoginError.InvalidUsername;
        }

        if (!_refreshTokenProviderMap.ContainsKey(request.RefreshTokenType))
        {
            return LoginError.InvalidTokenTypeRequested;
        }

        return GetValidClientPasswords(request.VersionedPasswords)
            .Map(x => new ValidLoginRequest(validUsername, x, request.RefreshTokenType));
    }

    private async Task<Either<LoginError, UserEntity>> GetUserAsync(ValidLoginRequest validLoginRequest)
    {
        _foundUserEntity = await _dataContext.Users
            .Where(x => x.Username == validLoginRequest.Username.Value)
            .Include(x => x.FailedLoginAttempts)
            .Include(x => x.MasterKey)
            .Include(x => x.KeyPair)
            .FirstOrDefaultAsync();

        if (_foundUserEntity is null)
        {
            return LoginError.InvalidUsername;
        }

        if (_foundUserEntity.FailedLoginAttempts!.Count >= MaximumFailedLoginAttempts)
        {
            return LoginError.ExcessiveFailedLoginAttempts;
        }

        return _foundUserEntity;
    }
    
    private Either<LoginError, Unit> VerifyAndUpgradePassword(ValidLoginRequest validLoginRequest, UserEntity userEntity)
    {
        bool requestContainsRequiredPasswordVersions = validLoginRequest.VersionedPasswords.ContainsKey(userEntity.ClientPasswordVersion)
                                                       && validLoginRequest.VersionedPasswords.ContainsKey(_clientPasswordVersion);
        if (!requestContainsRequiredPasswordVersions)
        {
            return LoginError.InvalidPasswordVersion;
        }

        // Get the appropriate 'existing' password for the user, based on the saved 'ClientPasswordVersion' for the user.
        // Then hash that password based on the saved 'ServerPasswordVersion' for the user.
        byte[] currentClientPassword = validLoginRequest.VersionedPasswords[userEntity.ClientPasswordVersion];
        bool isMatchingPassword = AuthenticationPassword.TryFrom(currentClientPassword, out AuthenticationPassword validAuthenticationPassword)
                                  && _passwordHashService.VerifySecurePasswordHash(validAuthenticationPassword, userEntity.PasswordHash, userEntity.PasswordSalt, userEntity.ServerPasswordVersion);
        if (!isMatchingPassword)
        {
            return LoginError.InvalidPassword;
        }

        // Now handle the case where even though the provided password is correct
        // the password must be upgraded to the latest 'ClientPasswordVersion' or 'ServerPasswordVersion'
        bool serverPasswordVersionIsOld = userEntity.ServerPasswordVersion != _passwordHashService.LatestServerPasswordVersion;
        bool clientPasswordVersionIsOld = userEntity.ClientPasswordVersion != _clientPasswordVersion;
        if (serverPasswordVersionIsOld || clientPasswordVersionIsOld)
        {
            byte[] latestClientPassword = validLoginRequest.VersionedPasswords[_clientPasswordVersion];
            if (!AuthenticationPassword.TryFrom(latestClientPassword, out AuthenticationPassword latestValidAuthenticationPassword))
            {
                return LoginError.InvalidPassword;
            }

            SecurePasswordHashOutput hashOutput = _passwordHashService.MakeSecurePasswordHash(latestValidAuthenticationPassword, _passwordHashService.LatestServerPasswordVersion);
            
            userEntity.PasswordHash = hashOutput.Hash;
            userEntity.PasswordSalt = hashOutput.Salt;
            userEntity.ServerPasswordVersion = _passwordHashService.LatestServerPasswordVersion;
            userEntity.ClientPasswordVersion = _clientPasswordVersion;
        }

        return Unit.Default;
    }

    private async Task<Either<LoginError, LoginResponse>> CreateLoginResponseAsync(UserEntity userEntity, TokenType refreshTokenType, string deviceDescription)
    {
        userEntity.LastLogin = DateTime.UtcNow;
        
        RefreshTokenData refreshToken = _refreshTokenProviderMap[refreshTokenType].Invoke(userEntity.Id);
        UserTokenEntity tokenEntity = new UserTokenEntity(
            refreshToken.TokenId, 
            userEntity.Id,
            deviceDescription,
            refreshTokenType,
            refreshToken.Created,
            refreshToken.Expiration);
        
        _dataContext.UserTokens.Add(tokenEntity);
        await _dataContext.SaveChangesAsync();
        
        string authToken = _tokenService.NewAuthenticationToken(userEntity.Id);
        await Common.PublishRefreshTokenCreatedEventAsync(_publisher, refreshToken);
        
        return new LoginResponse(userEntity.Username, authToken, refreshToken.Token);
    }
    
    private Either<LoginError, IDictionary<short, byte[]>> GetValidClientPasswords(List<VersionedPassword> clientPasswords)
    {
        bool noneMatchingCurrentClientPasswordVersion = clientPasswords.All(x => x.Version != _clientPasswordVersion);
        if (noneMatchingCurrentClientPasswordVersion)
        {
            return LoginError.InvalidPasswordVersion;
        }

        bool someHasInvalidClientPasswordVersion = clientPasswords.Any(x => x.Version > _clientPasswordVersion || x.Version < 0);
        if (someHasInvalidClientPasswordVersion)
        {
            return LoginError.InvalidPasswordVersion;
        }

        bool duplicateVersionsProvided = clientPasswords.GroupBy(x => x.Version).Any(x => x.Count() > 1);
        if (duplicateVersionsProvided)
        {
            return LoginError.InvalidPasswordVersion;
        }

        return clientPasswords.ToDictionary(x => x.Version, x => x.Password);
    }
}
