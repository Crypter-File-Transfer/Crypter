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
using System.Linq;
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
    Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest request, string deviceDescription);
    Task<Either<RefreshError, RefreshResponse>> RefreshAsync(ClaimsPrincipal claimsPrincipal, string deviceDescription);
    Task<Either<LogoutError, Unit>> LogoutAsync(ClaimsPrincipal claimsPrincipal);

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
    private const int MaximumFailedLoginAttempts = 3;

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="deviceDescription"></param>
    /// <returns></returns>
    /// <remarks>
    /// The reason this does not use Linq query syntax is to save a single trip to the database when querying for the user entity.
    /// `.Include(x => x.FailedLoginAttempts)` is less likely to be forgotten and break things when the reason for having it is on the very next line.
    /// </remarks>
    public async Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest request, string deviceDescription)
    {
        Either<LoginError, ValidLoginRequest> validLoginRequest = ValidateLoginRequest(request);
        return await validLoginRequest.MatchAsync<Either<LoginError, LoginResponse>>(
            error => error,
            async validatedLoginRequest =>
            {
                UserEntity? user = await _context.Users
                    .Where(x => x.Username == validatedLoginRequest.Username.Value)
                    .Include(x => x.FailedLoginAttempts)
                    .Include(x => x.Consents!.Where(y => y.Active == true))
                    .Include(x => x.MasterKey)
                    .Include(x => x.KeyPair)
                    .FirstOrDefaultAsync();

                if (user is null)
                {
                    return LoginError.InvalidUsername;
                }

                if (user.FailedLoginAttempts!.Count >= MaximumFailedLoginAttempts)
                {
                    return LoginError.ExcessiveFailedLoginAttempts;
                }

                bool requestContainsRequiredPasswordVersions =
                    validatedLoginRequest.VersionedPasswords.ContainsKey(user.ClientPasswordVersion)
                    && validatedLoginRequest.VersionedPasswords.ContainsKey(_clientPasswordVersion);
                if (!requestContainsRequiredPasswordVersions)
                {
                    return LoginError.InvalidPasswordVersion;
                }

                byte[] currentClientPassword = validatedLoginRequest.VersionedPasswords[user.ClientPasswordVersion];
                bool isMatchingPassword = AuthenticationPassword.TryFrom(currentClientPassword, out AuthenticationPassword validAuthenticationPassword)
                    && _passwordHashService.VerifySecurePasswordHash(validAuthenticationPassword,
                        user.PasswordHash, user.PasswordSalt, user.ServerPasswordVersion);
                if (!isMatchingPassword)
                {
                    await HandlePasswordVerificationFailedAsync(user.Id);
                    return LoginError.InvalidPassword;
                }

                if (user.ServerPasswordVersion != _passwordHashService.LatestServerPasswordVersion ||
                    user.ClientPasswordVersion != _clientPasswordVersion)
                {
                    byte[] latestClientPassword = validatedLoginRequest.VersionedPasswords[_clientPasswordVersion];
                    if (!AuthenticationPassword.TryFrom(latestClientPassword,
                            out AuthenticationPassword latestValidAuthenticationPassword))
                    {
                        return LoginError.InvalidPassword;
                    }
                    
                    SecurePasswordHashOutput hashOutput = _passwordHashService.MakeSecurePasswordHash(latestValidAuthenticationPassword,
                        _passwordHashService.LatestServerPasswordVersion);
                    user.PasswordHash = hashOutput.Hash;
                    user.PasswordSalt = hashOutput.Salt;
                    user.ServerPasswordVersion = _passwordHashService.LatestServerPasswordVersion;
                    user.ClientPasswordVersion = _clientPasswordVersion;
                }

                user.LastLogin = DateTime.UtcNow;

                RefreshTokenData refreshToken =
                    CreateRefreshTokenInContext(user.Id, validatedLoginRequest.RefreshTokenType, deviceDescription);
                string authToken = MakeAuthenticationToken(user.Id);

                await _context.SaveChangesAsync();
                ScheduleRefreshTokenDeletion(refreshToken.TokenId, refreshToken.Expiration);

                bool userHasConsentedToRecoveryKeyRisks =
                    user.Consents!.Any(x => x.ConsentType == ConsentType.RecoveryKeyRisks);
                bool userNeedsNewKeys = user.MasterKey is null && user.KeyPair is null;

                return new LoginResponse(user.Username, authToken, refreshToken.Token, userNeedsNewKeys,
                    !userHasConsentedToRecoveryKeyRisks);
            },
            LoginError.UnknownError);
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

    public Task<Either<LogoutError, Unit>> LogoutAsync(ClaimsPrincipal claimsPrincipal)
    {
        return from userId in TokenService.TryParseUserId(claimsPrincipal).ToEither(LogoutError.InvalidToken).AsTask()
            from tokenId in TokenService.TryParseTokenId(claimsPrincipal).ToEither(LogoutError.InvalidToken).AsTask()
            from databaseToken in FetchUserTokenAsync(tokenId).ToEitherAsync(LogoutError.InvalidToken)
            let databaseTokenDeleted = DeleteUserTokenInContext(databaseToken)
            from entriesModified in Either<LogoutError, int>.FromRightAsync(SaveContextChangesAsync())
            select Unit.Default;
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

    private Either<LoginError, ValidLoginRequest> ValidateLoginRequest(LoginRequest request)
    {
        if (request.VersionedPasswords.All(x => x.Version != _clientPasswordVersion))
        {
            return LoginError.InvalidPasswordVersion;
        }

        if (!Username.TryFrom(request.Username, out var validUsername))
        {
            return LoginError.InvalidUsername;
        }

        Dictionary<int, byte[]> validVersionedPasswords = new Dictionary<int, byte[]>(request.VersionedPasswords.Count);
        foreach (VersionedPassword versionedPassword in request.VersionedPasswords)
        {
            if (versionedPassword.Version > _clientPasswordVersion || versionedPassword.Version < 0 ||
                validVersionedPasswords.ContainsKey(versionedPassword.Version))
            {
                return LoginError.InvalidPasswordVersion;
            }

            validVersionedPasswords.Add(versionedPassword.Version, versionedPassword.Password);
        }

        if (!_refreshTokenProviderMap.ContainsKey(request.RefreshTokenType))
        {
            return LoginError.InvalidTokenTypeRequested;
        }

        return new ValidLoginRequest(validUsername, validVersionedPasswords, request.RefreshTokenType);
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

    private async Task HandlePasswordVerificationFailedAsync(Guid userId)
    {
        UserFailedLoginEntity failedLoginEntity = new UserFailedLoginEntity(Guid.NewGuid(), userId, DateTime.UtcNow);
        _context.UserFailedLoginAttempts.Add(failedLoginEntity);
        await _context.SaveChangesAsync(CancellationToken.None);
        _backgroundJobClient.Schedule(
            () => _hangfireBackgroundService.DeleteFailedLoginAttemptAsync(failedLoginEntity.Id),
            failedLoginEntity.Date.AddDays(1));
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

    private record ValidLoginRequest
    {
        public Username Username { get; init; }
        public Dictionary<int, byte[]> VersionedPasswords { get; init; }
        public TokenType RefreshTokenType { get; init; }

        public ValidLoginRequest(Username username, Dictionary<int, byte[]> versionedPasswords,
            TokenType refreshTokenType)
        {
            Username = username;
            VersionedPasswords = versionedPasswords;
            RefreshTokenType = refreshTokenType;
        }
    }
}
