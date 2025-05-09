/*
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
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OneOf;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.UserAuthentication.Commands;

public sealed record UserLoginCommand(LoginRequest Request, string DeviceDescription) : IEitherRequest<LoginError, OneOf<ChallengeResponse, LoginResponse>>;

internal sealed class UserLoginCommandHandler : IEitherRequestHandler<UserLoginCommand, LoginError, OneOf<ChallengeResponse, LoginResponse>>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly DataContext _dataContext;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;
    private readonly IHashIdService _hashIdService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPublisher _publisher;
    private readonly ITokenService _tokenService;

    private const int MultiFactorExpirationMinutes = 5;
    private readonly short _clientPasswordVersion;
    private const int MaximumFailedLoginAttempts = 3;
    private readonly Dictionary<TokenType, Func<Guid, RefreshTokenData>> _refreshTokenProviderMap;

    private readonly DateTimeOffset _currentTime;
    private UserEntity? _foundUserEntity;

    public UserLoginCommandHandler(IBackgroundJobClient backgroundJobClient, DataContext dataContext, IHangfireBackgroundService hangfireBackgroundService, IHashIdService hashIdService, IOptions<ServerPasswordSettings> passwordSettings, IPasswordHashService passwordHashService, IPublisher publisher, ITokenService tokenService)
    {
        _backgroundJobClient = backgroundJobClient;
        _dataContext = dataContext;
        _hangfireBackgroundService = hangfireBackgroundService;
        _hashIdService = hashIdService;
        _passwordHashService = passwordHashService;
        _publisher = publisher;
        _tokenService = tokenService;
        
        _clientPasswordVersion = passwordSettings.Value.ClientVersion;
        _refreshTokenProviderMap = new Dictionary<TokenType, Func<Guid, RefreshTokenData>>
        {
            { TokenType.Session, _tokenService.NewSessionToken },
            { TokenType.Device, _tokenService.NewDeviceToken }
        };

        _currentTime = DateTimeOffset.UtcNow;
    }
    
    public async Task<Either<LoginError, OneOf<ChallengeResponse, LoginResponse>>> Handle(UserLoginCommand request, CancellationToken cancellationToken)
    {
        return await ValidateLoginRequest(request.Request)
            .BindAsync(async validLoginRequest => await (
                from foundUser in GetUserAsync(validLoginRequest)
                from passwordVerificationSuccess in VerifyPassword(validLoginRequest, foundUser).AsTask()
                from twoFactorAuthenticationRequired in CheckMultiFactorAuthentication(foundUser, validLoginRequest.ValidMultiFactorVerification).AsTask()
                select twoFactorAuthenticationRequired.MapT1(_ => validLoginRequest)))
            .BindAsync(async preliminaryLoginResult => {
                if (preliminaryLoginResult.TryPickT0(out Guid challengeId, out ValidLoginRequest validLoginRequest))
                {
                    _backgroundJobClient.Enqueue(() => _hangfireBackgroundService.SendMultiFactorVerificationCodeAsync(_foundUserEntity!.Id, challengeId, MultiFactorExpirationMinutes));
                    string challengeHash = _hashIdService.Encode(challengeId);
                    ChallengeResponse challengeResponse = new ChallengeResponse(challengeHash);
                    return OneOf<ChallengeResponse, LoginResponse>.FromT0(challengeResponse);
                }

                return await (
                    from passwordUpgradePerformed in UpgradePasswordIfRequired(validLoginRequest, _foundUserEntity!).AsTask()
                    from loginResponse in CreateLoginResponseAsync(_foundUserEntity!, validLoginRequest.RefreshTokenType, request.DeviceDescription)
                    select OneOf<ChallengeResponse, LoginResponse>.FromT1(loginResponse));
            })
            .DoRightAsync(async _  =>
            {
                SuccessfulUserLoginEvent successfulUserLoginEvent = new SuccessfulUserLoginEvent(_foundUserEntity!.Id, request.DeviceDescription, _currentTime);
                await _publisher.Publish(successfulUserLoginEvent, CancellationToken.None);
            })
            .DoLeftOrNeitherAsync(async error =>
            {
                FailedUserLoginEvent failedUserLoginEvent = new FailedUserLoginEvent(request.Request.Username, error, request.DeviceDescription, _currentTime);
                await _publisher.Publish(failedUserLoginEvent, CancellationToken.None);

                if (error == LoginError.InvalidPassword)
                {
                    IncorrectPasswordProvidedEvent incorrectPasswordProvidedEvent = new IncorrectPasswordProvidedEvent(_foundUserEntity!.Id);
                    await _publisher.Publish(incorrectPasswordProvidedEvent, CancellationToken.None);
                }
            },
            async () =>
            {
                FailedUserLoginEvent failedUserLoginEvent = new FailedUserLoginEvent(request.Request.Username, LoginError.UnknownError, request.DeviceDescription, _currentTime);
                await _publisher.Publish(failedUserLoginEvent, CancellationToken.None);
            });
    }
    
    private record ValidLoginRequest(Username Username, IDictionary<short, byte[]> VersionedPasswords, TokenType RefreshTokenType, ValidMultiFactorVerification? ValidMultiFactorVerification)
    {
        public Username Username { get; } = Username;
        public IDictionary<short, byte[]> VersionedPasswords { get; } = VersionedPasswords;
        public TokenType RefreshTokenType { get; } = RefreshTokenType;
        public ValidMultiFactorVerification? ValidMultiFactorVerification { get; } = ValidMultiFactorVerification;
    }

    private record ValidMultiFactorVerification(Guid ChallengeId, string VerificationCode)
    {
        public Guid ChallengeId { get; } = ChallengeId;
        public string VerificationCode { get; } = VerificationCode;
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
            .Bind(passwordMap =>
            {
                if (request.MultiFactorVerification is null)
                {
                    return new ValidLoginRequest(validUsername, passwordMap, request.RefreshTokenType, null);
                }

                return ValidateMultiFactorVerification(request.MultiFactorVerification)
                    .Map(validMultiFactorVerification => new ValidLoginRequest(validUsername, passwordMap, request.RefreshTokenType, validMultiFactorVerification));
            });
    }

    private Either<LoginError, ValidMultiFactorVerification> ValidateMultiFactorVerification(MultiFactorVerification multiFactorVerification)
    {
        return _hashIdService.Decode(multiFactorVerification.ChallengeHash)
            .Select(x => new ValidMultiFactorVerification(x, multiFactorVerification.VerificationCode))
            .ToEither(LoginError.InvalidMultiFactorChallenge);
    }
    
    private async Task<Either<LoginError, UserEntity>> GetUserAsync(ValidLoginRequest validLoginRequest)
    {
        _foundUserEntity = await _dataContext.Users
            .Where(x => x.Username == validLoginRequest.Username.Value)
            .Include(x => x.FailedLoginAttempts)
            .Include(x => x.MasterKey)
            .Include(x => x.KeyPair)
            .Include(x => x.MultiFactorChallenges)
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

    private Either<LoginError, Unit> VerifyPassword(ValidLoginRequest validLoginRequest, UserEntity userEntity)
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

        return Unit.Default;
    }
    
    /// <summary>
    /// If the user account requires MFA, and a valid MFA is not provided, then return a new Challenge Id.
    /// If the user account does not require MFA, then return an indication of a successful check.
    /// If the user account requires MFA, and a valid MFA is provided, then verify the MFA is valid and correct.
    /// </summary>
    /// <param name="userEntity"></param>
    /// <param name="validMultiFactorVerification"></param>
    /// <returns></returns>
    private static Either<LoginError, OneOf<Guid, Unit>> CheckMultiFactorAuthentication(UserEntity userEntity, ValidMultiFactorVerification? validMultiFactorVerification)
    {
        if (userEntity is { RequireTwoFactorAuthentication: true, EmailAddress: not null })
        {
            if (validMultiFactorVerification is null)
            {
                return OneOf<Guid, Unit>.FromT0(Guid.NewGuid());
            }

            UserMultiFactorChallengeEntity? challengeEntity = userEntity.MultiFactorChallenges!
                .Where(x => x.Id == validMultiFactorVerification.ChallengeId)
                .Where(x => x.VerificationCode == validMultiFactorVerification.VerificationCode)
                .Where(x => x.Created <= DateTime.UtcNow.AddMinutes(MultiFactorExpirationMinutes))
                .FirstOrDefault();

            if (challengeEntity is null)
            {
                return LoginError.InvalidMultiFactorChallenge;
            }

            userEntity.MultiFactorChallenges!.Remove(challengeEntity);
        }

        return OneOf<Guid, Unit>.FromT1(Unit.Default);
    }
    
    private Either<LoginError, Unit> UpgradePasswordIfRequired(ValidLoginRequest validLoginRequest, UserEntity userEntity)
    {
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
        userEntity.LastLogin = _currentTime.UtcDateTime;
        
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
