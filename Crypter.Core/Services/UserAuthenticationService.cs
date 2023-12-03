/*
 * Copyright (C) 2023 Crypter File Transfer
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
using Crypter.Core.DataContextExtensions;
using Crypter.Core.Features.UserAuthentication;
using Crypter.Core.Identity;
using Crypter.Core.Models;
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
    Task<Either<RegistrationError, Unit>> RegisterAsync(RegistrationRequest request);
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
    private const int _maximumFailedLoginAttempts = 3;

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

    public Task<Either<RegistrationError, Unit>> RegisterAsync(RegistrationRequest request)
    {
        return from validRegistrationRequest in ValidateRegistrationRequest(request).AsTask()
            from usernameAvailable in VerifyUsernameIsAvailableAsync(validRegistrationRequest.Username,
                RegistrationError.UsernameTaken)
            from emailAddressAvailable in VerifyEmailAddressIsAvailable(validRegistrationRequest.EmailAddress,
                RegistrationError.EmailAddressTaken)
            let securePasswordData = _passwordHashService.MakeSecurePasswordHash(validRegistrationRequest.Password,
                _passwordHashService.LatestServerPasswordVersion)
            from newUserEntity in Either<RegistrationError, UserEntity>.FromRight(
                InsertNewUserInContext(validRegistrationRequest.Username, validRegistrationRequest.EmailAddress,
                    securePasswordData.Salt, securePasswordData.Hash, _passwordHashService.LatestServerPasswordVersion,
                    _clientPasswordVersion)).AsTask()
            from entriesModified in Either<RegistrationError, int>.FromRightAsync(SaveContextChangesAsync())
            let jobId = EnqueueEmailAddressVerificationEmailDelivery(newUserEntity.Id)
            select Unit.Default;
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
        var validLoginRequest = ValidateLoginRequest(request);
        return await validLoginRequest.MatchAsync<Either<LoginError, LoginResponse>>(
            error => error,
            async validatedLoginRequest =>
            {
                UserEntity user = await _context.Users
                    .Where(x => x.Username == validatedLoginRequest.Username.Value)
                    .Include(x => x.FailedLoginAttempts)
                    .Include(x => x.Consents.Where(y => y.Active == true))
                    .Include(x => x.MasterKey)
                    .Include(x => x.KeyPair)
                    .FirstOrDefaultAsync();

                if (user is null)
                {
                    return LoginError.InvalidUsername;
                }

                if (user.FailedLoginAttempts.Count >= _maximumFailedLoginAttempts)
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
                bool isMatchingPassword = _passwordHashService.VerifySecurePasswordHash(currentClientPassword,
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
                    var hashOutput = _passwordHashService.MakeSecurePasswordHash(latestClientPassword,
                        _passwordHashService.LatestServerPasswordVersion);
                    user.PasswordHash = hashOutput.Hash;
                    user.PasswordSalt = hashOutput.Salt;
                    user.ServerPasswordVersion = _passwordHashService.LatestServerPasswordVersion;
                    user.ClientPasswordVersion = _clientPasswordVersion;
                }

                user.LastLogin = DateTime.UtcNow;

                var refreshToken =
                    CreateRefreshTokenInContext(user.Id, validatedLoginRequest.RefreshTokenType, deviceDescription);
                var authToken = MakeAuthenticationToken(user.Id);

                await _context.SaveChangesAsync();
                ScheduleRefreshTokenDeletion(refreshToken.TokenId, refreshToken.Expiration);

                bool userHasConsentedToRecoveryKeyRisks =
                    user.Consents.Any(x => x.ConsentType == ConsentType.RecoveryKeyRisks);
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
        return from suppliedPassword in ValidateRequestPassword(request.Password,
                PasswordChallengeError.InvalidPassword).AsTask()
            from user in FetchUserAsync(userId, PasswordChallengeError.UnknownError, cancellationToken)
            from unit0 in VerifyUserPasswordIsMigrated(user, PasswordChallengeError.PasswordNeedsMigration)
                .ToLeftEither(Unit.Default).AsTask()
            from passwordVerified in VerifyPassword(suppliedPassword, user.PasswordHash, user.PasswordSalt,
                _passwordHashService.LatestServerPasswordVersion)
                ? Either<PasswordChallengeError, Unit>.FromRight(Unit.Default).AsTask()
                : Either<PasswordChallengeError, Unit>.FromLeft(PasswordChallengeError.InvalidPassword).AsTask()
            select Unit.Default;
    }

    private Either<LoginError, ValidLoginRequest> ValidateLoginRequest(LoginRequest request)
    {
        if (!request.VersionedPasswords.Any(x => x.Version == _clientPasswordVersion))
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
            else
            {
                validVersionedPasswords.Add(versionedPassword.Version, versionedPassword.Password);
            }
        }

        if (!_refreshTokenProviderMap.ContainsKey(request.RefreshTokenType))
        {
            return LoginError.InvalidTokenTypeRequested;
        }

        return new ValidLoginRequest(validUsername, validVersionedPasswords, request.RefreshTokenType);
    }

    private Either<RegistrationError, ValidRegistrationRequest> ValidateRegistrationRequest(RegistrationRequest request)
    {
        if (request.VersionedPassword.Version != _clientPasswordVersion)
        {
            return RegistrationError.OldPasswordVersion;
        }

        if (!Username.TryFrom(request.Username, out var validUsername))
        {
            return RegistrationError.InvalidUsername;
        }

        if (!ValidateRequestPassword(request.VersionedPassword.Password, RegistrationError.InvalidPassword).IsRight)
        {
            return RegistrationError.InvalidPassword;
        }

        bool isPossibleEmailAddress = !string.IsNullOrEmpty(request.EmailAddress);
        Maybe<EmailAddress> validatedEmailAddress =
            EmailAddress.TryFrom(request.EmailAddress, out var validEmailAddressOrNull)
                ? validEmailAddressOrNull
                : Maybe<EmailAddress>.None;

        if (isPossibleEmailAddress && validatedEmailAddress.IsNone)
        {
            return RegistrationError.InvalidEmailAddress;
        }

        return new ValidRegistrationRequest(validUsername, request.VersionedPassword.Password, validatedEmailAddress);
    }

    private static Either<T, byte[]> ValidateRequestPassword<T>(byte[] password, T error)
    {
        return UserAuthenticationValidators.ValidatePassword(password)
            ? password
            : error;
    }

    private Maybe<T> VerifyUserPasswordIsMigrated<T>(UserEntity user, T error)
    {
        return user.ClientPasswordVersion == _clientPasswordVersion &&
               user.ServerPasswordVersion == _passwordHashService.LatestServerPasswordVersion
            ? Maybe<T>.None
            : error;
    }

    private async Task<Either<T, Unit>> VerifyUsernameIsAvailableAsync<T>(Username username, T error,
        CancellationToken cancellationToken = default)
    {
        bool isUsernameAvailable = await _context.Users.IsUsernameAvailableAsync(username, cancellationToken);
        return isUsernameAvailable
            ? Unit.Default
            : error;
    }

    private async Task<Either<T, Unit>> VerifyEmailAddressIsAvailable<T>(Maybe<EmailAddress> emailAddress, T error,
        CancellationToken cancellationToken = default)
    {
        return await emailAddress.MatchAsync<Either<T, Unit>>(
            () => Unit.Default,
            async x =>
            {
                bool isEmailAddressAvailable = await _context.Users.IsEmailAddressAvailableAsync(x, cancellationToken);
                return isEmailAddressAvailable
                    ? Unit.Default
                    : error;
            });
    }

    private static Maybe<Unit> ValidateUserToken(UserTokenEntity token, Guid userId)
    {
        bool isTokenValid = token.Owner == userId
                            && token.Expiration >= DateTime.UtcNow;

        return isTokenValid
            ? Unit.Default
            : Maybe<Unit>.None;
    }

    private UserEntity InsertNewUserInContext(Username username, Maybe<EmailAddress> emailAddress, byte[] passwordSalt,
        byte[] passwordHash, short serverPasswordVersion, short clientPasswordVersion)
    {
        UserEntity user = new UserEntity(Guid.NewGuid(), username, emailAddress, passwordHash, passwordSalt,
            serverPasswordVersion, clientPasswordVersion, false, DateTime.UtcNow, DateTime.MinValue);
        user.Profile = new UserProfileEntity(user.Id, string.Empty, string.Empty, string.Empty);
        user.PrivacySetting = new UserPrivacySettingEntity(user.Id, true, UserVisibilityLevel.Everyone,
            UserItemTransferPermission.Everyone, UserItemTransferPermission.Everyone);
        user.NotificationSetting = new UserNotificationSettingEntity(user.Id, false, false);

        _context.Users.Add(user);
        return user;
    }

    private Task<Either<T, UserEntity>> FetchUserAsync<T>(Guid userId, T error,
        CancellationToken cancellationToken = default)
    {
        return Either<T, UserEntity>.FromRightAsync(
            _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken),
            error);
    }

    private bool VerifyPassword(byte[] password, byte[] existingPasswordHash, byte[] passwordSalt,
        short serverPasswordVersion)
    {
        return _passwordHashService.VerifySecurePasswordHash(password, existingPasswordHash, passwordSalt,
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
        return Maybe<UserTokenEntity>.FromAsync(_context.UserTokens
            .FindAsync(new object[] { tokenId }, cancellationToken).AsTask());
    }

    private Unit DeleteUserTokenInContext(UserTokenEntity token)
    {
        _context.UserTokens.Remove(token);
        return Unit.Default;
    }

    private string EnqueueEmailAddressVerificationEmailDelivery(Guid userId)
    {
        return _backgroundJobClient.Enqueue(() => _hangfireBackgroundService.SendEmailVerificationAsync(userId));
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

    private record ValidRegistrationRequest
    {
        public Username Username { get; init; }
        public byte[] Password { get; init; }
        public Maybe<EmailAddress> EmailAddress { get; init; }

        public ValidRegistrationRequest(Username username, byte[] password, Maybe<EmailAddress> emailAddress)
        {
            Username = username;
            Password = password;
            EmailAddress = emailAddress;
        }
    }
}
