/*
 * Copyright (C) 2024 Crypter File Transfer
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
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Core.Identity;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Services;

public interface IUserAuthenticationService
{
    Task<Either<RefreshError, RefreshResponse>> RefreshAsync(ClaimsPrincipal claimsPrincipal, string deviceDescription);
    Task<Either<PasswordChallengeError, Unit>> TestUserPasswordAsync(Guid userId, PasswordChallengeRequest request,
        CancellationToken cancellationToken = default);
}

public static class UserAuthenticationServiceExtensions
{
    public static void AddUserAuthenticationService(this IServiceCollection services,
        Action<ServerPasswordSettings> settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        services.Configure(settings);
        services.TryAddScoped<IUserAuthenticationService, UserAuthenticationService>();
    }
}

public class UserAuthenticationService : IUserAuthenticationService
{
    private readonly DataContext _context;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenService _tokenService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;
    private readonly IReadOnlyDictionary<TokenType, Func<Guid, RefreshTokenData>> _refreshTokenProviderMap;

    private readonly short _clientPasswordVersion;

    public UserAuthenticationService(DataContext context, IPasswordHashService passwordHashService,
        ITokenService tokenService, IBackgroundJobClient backgroundJobClient,
        IHangfireBackgroundService hangfireBackgroundService, IOptions<ServerPasswordSettings> passwordSettings)
    {
        _context = context;
        _passwordHashService = passwordHashService;
        _tokenService = tokenService;
        _backgroundJobClient = backgroundJobClient;
        _hangfireBackgroundService = hangfireBackgroundService;

        _clientPasswordVersion = passwordSettings.Value.ClientVersion;
        _refreshTokenProviderMap = new Dictionary<TokenType, Func<Guid, RefreshTokenData>>()
        {
            { TokenType.Session, _tokenService.NewSessionToken },
            { TokenType.Device, _tokenService.NewDeviceToken }
        };
    }

    public Task<Either<RefreshError, RefreshResponse>> RefreshAsync(ClaimsPrincipal claimsPrincipal,
        string deviceDescription)
    {
        return from userId in TokenService.TryParseUserId(claimsPrincipal).ToEither(RefreshError.InvalidToken).AsTask()
            from tokenId in TokenService.TryParseTokenId(claimsPrincipal).ToEither(RefreshError.InvalidToken).AsTask()
            from databaseToken in FetchUserTokenAsync(tokenId).ToEitherAsync(RefreshError.InvalidToken)
            from databaseTokenValidated in ValidateUserToken(databaseToken, userId).ToEither(RefreshError.InvalidToken)
                .AsTask()
            let databaseTokenDeleted = DeleteUserTokenInContext(databaseToken)
            from foundUser in FetchUserAsync(userId, RefreshError.UserNotFound)
            let lastLoginTimeUpdated = UpdateLastLoginTimeInContext(foundUser)
            let newRefreshTokenData = CreateRefreshTokenInContext(userId, databaseToken.Type, deviceDescription)
            let authenticationToken = MakeAuthenticationToken(userId)
            from entriesModified in Either<RefreshError, int>.FromRightAsync(SaveContextChangesAsync())
            let jobId = ScheduleRefreshTokenDeletion(newRefreshTokenData.TokenId, newRefreshTokenData.Expiration)
            select new RefreshResponse(authenticationToken, newRefreshTokenData.Token, databaseToken.Type);
    }

    public Task<Either<PasswordChallengeError, Unit>> TestUserPasswordAsync(Guid userId,
        PasswordChallengeRequest request, CancellationToken cancellationToken = default)
    {
        return from validAuthenticationPassword in ValidateRequestPassword(request.AuthenticationPassword,
                PasswordChallengeError.InvalidPassword).AsTask()
            from user in FetchUserAsync(userId, PasswordChallengeError.UnknownError, cancellationToken)
            from unit0 in VerifyUserPasswordIsMigrated(user, PasswordChallengeError.PasswordNeedsMigration)
                .ToLeftEither(Unit.Default).AsTask()
            from passwordVerified in VerifyPassword(validAuthenticationPassword, user.PasswordHash, user.PasswordSalt,
                _passwordHashService.LatestServerPasswordVersion)
                ? Either<PasswordChallengeError, Unit>.FromRight(Unit.Default).AsTask()
                : Either<PasswordChallengeError, Unit>.FromLeft(PasswordChallengeError.InvalidPassword).AsTask()
            select Unit.Default;
    }

    private static Either<T, AuthenticationPassword> ValidateRequestPassword<T>(byte[] password, T error)
    {
        return AuthenticationPassword.TryFrom(password, out AuthenticationPassword validAuthenticationPassword)
            ? validAuthenticationPassword
            : error;
    }

    private Maybe<T> VerifyUserPasswordIsMigrated<T>(UserEntity user, T error)
    {
        return user.ClientPasswordVersion == _clientPasswordVersion &&
               user.ServerPasswordVersion == _passwordHashService.LatestServerPasswordVersion
            ? Maybe<T>.None
            : error;
    }

    private static Maybe<Unit> ValidateUserToken(UserTokenEntity token, Guid userId)
    {
        bool isTokenValid = token.Owner == userId
                            && token.Expiration >= DateTime.UtcNow;

        return isTokenValid
            ? Unit.Default
            : Maybe<Unit>.None;
    }

    private Task<Either<T, UserEntity?>> FetchUserAsync<T>(Guid userId, T error,
        CancellationToken cancellationToken = default)
    {
        return Either<T, UserEntity?>.FromRightAsync(
            _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken),
            error);
    }

    private bool VerifyPassword(AuthenticationPassword authenticationPassword, byte[] existingPasswordHash, byte[] passwordSalt,
        short serverPasswordVersion)
    {
        return _passwordHashService.VerifySecurePasswordHash(authenticationPassword, existingPasswordHash, passwordSalt,
            serverPasswordVersion);
    }

    private static Unit UpdateLastLoginTimeInContext(UserEntity user)
    {
        user.LastLogin = DateTime.UtcNow;
        return Unit.Default;
    }

    private Task<Maybe<UserTokenEntity>> FetchUserTokenAsync(Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        return Maybe<UserTokenEntity>.FromNullableAsync(_context.UserTokens
            .FindAsync([tokenId], cancellationToken).AsTask());
    }

    private Unit DeleteUserTokenInContext(UserTokenEntity token)
    {
        _context.UserTokens.Remove(token);
        return Unit.Default;
    }

    private string ScheduleRefreshTokenDeletion(Guid tokenId, DateTime tokenExpiration)
    {
        return _backgroundJobClient.Schedule(() => _hangfireBackgroundService.DeleteUserTokenAsync(tokenId),
            tokenExpiration - DateTime.UtcNow);
    }

    private string MakeAuthenticationToken(Guid userId)
    {
        return _tokenService.NewAuthenticationToken(userId);
    }

    private RefreshTokenData CreateRefreshTokenInContext(Guid userId, TokenType tokenType, string deviceDescription)
    {
        RefreshTokenData refreshToken = _refreshTokenProviderMap[tokenType].Invoke(userId);
        UserTokenEntity tokenEntity = new(refreshToken.TokenId, userId, deviceDescription, tokenType,
            refreshToken.Created, refreshToken.Expiration);
        _context.UserTokens.Add(tokenEntity);
        return refreshToken;
    }

    private Task<int> SaveContextChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
