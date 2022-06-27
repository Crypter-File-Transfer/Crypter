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

using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication;
using Crypter.Contracts.Features.Settings;
using Crypter.Core.DataContextExtensions;
using Crypter.Core.Entities;
using Crypter.Core.Identity;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserAuthenticationService
   {
      Task<Either<RegistrationError, RegistrationResponse>> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken);
      Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest request, string deviceDescription, CancellationToken cancellationToken);
      Task<Either<RefreshError, RefreshResponse>> RefreshAsync(ClaimsPrincipal claimsPrincipal, string deviceDescription, CancellationToken cancellationToken);
      Task<Either<LogoutError, LogoutResponse>> LogoutAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken);
      Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateUserContactInfoAsync(Guid userId, UpdateContactInfoRequest request, CancellationToken cancellationToken);
   }

   public class UserAuthenticationService : IUserAuthenticationService
   {
      private readonly DataContext _context;
      private readonly IPasswordHashService _passwordHashService;
      private readonly ITokenService _tokenService;
      private readonly IHangfireBackgroundService _hangfireBackgroundService;
      private readonly IReadOnlyDictionary<TokenType, Func<Guid, RefreshTokenData>> _refreshTokenProviderMap;

      public UserAuthenticationService(DataContext context, IPasswordHashService passwordHashService, ITokenService tokenService, IHangfireBackgroundService hangfireBackgroundService)
      {
         _context = context;
         _passwordHashService = passwordHashService;
         _tokenService = tokenService;
         _hangfireBackgroundService = hangfireBackgroundService;

         _refreshTokenProviderMap = new Dictionary<TokenType, Func<Guid, RefreshTokenData>>()
         {
            { TokenType.Session, _tokenService.NewSessionToken },
            { TokenType.Device, _tokenService.NewDeviceToken }
         };
      }

      public Task<Either<RegistrationError, RegistrationResponse>> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
      {
         return from validRegistrationRequest in ValidateRegistrationRequest(request).AsTask()
                from isUsernameAvailable in Either<RegistrationError, bool>.FromRightAsync(
                   VerifyUsernameIsAvailableAsync(validRegistrationRequest.Username, cancellationToken))
                from usernameAvailabilityCheck in isUsernameAvailable
                  ? Either<RegistrationError, Unit>.FromRight(Unit.Default).AsTask()
                  : Either<RegistrationError, Unit>.FromLeft(RegistrationError.UsernameTaken).AsTask()
                from isEmailAddressAvailable in Either<RegistrationError, bool>.FromRightAsync(
                  validRegistrationRequest.EmailAddress.MatchAsync(
                     () => true,
                     async some => await VerifyEmailIsAddressAvailable(some, cancellationToken)))
                from emailAddressAvailabilityCheck in isEmailAddressAvailable
                  ? Either<RegistrationError, Unit>.FromRight(Unit.Default).AsTask()
                  : Either<RegistrationError, Unit>.FromLeft(RegistrationError.EmailAddressTaken).AsTask()
                let securePasswordData = _passwordHashService.MakeSecurePasswordHash(validRegistrationRequest.Password)
                from newUserEntity in Either<RegistrationError, UserEntity>.FromRight(InsertNewUser(validRegistrationRequest.Username, validRegistrationRequest.EmailAddress, securePasswordData.Salt, securePasswordData.Hash)).AsTask()
                from entriesModified in Either<RegistrationError, int>.FromRightAsync(SaveChangesAsync(cancellationToken))
                let jobId = EnqueueEmailAddressVerificationEmailDelivery(newUserEntity.Id)
                select new RegistrationResponse();
      }

      public Task<Either<LoginError, LoginResponse>> LoginAsync(LoginRequest request, string deviceDescription, CancellationToken cancellationToken)
      {
         return from validLoginRequest in ValidateLoginRequest(request).AsTask()
                from foundUser in FetchUserAsync(validLoginRequest, cancellationToken)
                from passwordVerified in VerifyPassword(validLoginRequest.Password, foundUser.PasswordHash, foundUser.PasswordSalt)
                  ? Either<LoginError, Unit>.FromRight(Unit.Default).AsTask()
                  : Either<LoginError, Unit>.FromLeft(LoginError.InvalidPassword).AsTask()
                let lastLoginTimeUpdated = UpdateLastLoginTime(foundUser)
                let refreshTokenData = MakeRefreshToken(foundUser.Id, validLoginRequest.RefreshTokenType)
                let refreshTokenStored = StoreRefreshToken(foundUser.Id, validLoginRequest.RefreshTokenType, refreshTokenData, deviceDescription)
                let authenticationToken = MakeAuthenticationToken(foundUser.Id)
                from entriesModified in Either<LoginError, int>.FromRightAsync(SaveChangesAsync(cancellationToken))
                let jobId = ScheduleRefreshTokenDeletion(refreshTokenData.TokenId, refreshTokenData.Expiration)
                select new LoginResponse(foundUser.Username, authenticationToken, refreshTokenData.Token);
      }

      public Task<Either<RefreshError, RefreshResponse>> RefreshAsync(ClaimsPrincipal claimsPrincipal, string deviceDescription, CancellationToken cancellationToken)
      {
         return from userId in ParseUserId(claimsPrincipal).ToEither(RefreshError.InvalidToken).AsTask()
                from tokenId in ParseRefreshTokenId(claimsPrincipal).ToEither(RefreshError.InvalidToken).AsTask()
                from databaseToken in FetchUserTokenAsync(tokenId, cancellationToken).ToEitherAsync(RefreshError.InvalidToken)
                from databaseTokenValidated in ValidateUserToken(databaseToken, userId).ToEither(RefreshError.InvalidToken).AsTask()
                let databaseTokenDeleted = DeleteUserToken(databaseToken)
                from foundUser in FetchUserAsync(userId, cancellationToken).ToEitherAsync(RefreshError.UserNotFound)
                let lastLoginTimeUpdated = UpdateLastLoginTime(foundUser)
                let newRefreshTokenData = MakeRefreshToken(userId, databaseToken.Type)
                let newRefreshTokenStored = StoreRefreshToken(userId, databaseToken.Type, newRefreshTokenData, deviceDescription)
                let authenticationToken = MakeAuthenticationToken(userId)
                from entriesModified in Either<RefreshError, int>.FromRightAsync(SaveChangesAsync(cancellationToken))
                let jobId = ScheduleRefreshTokenDeletion(newRefreshTokenData.TokenId, newRefreshTokenData.Expiration)
                select new RefreshResponse(authenticationToken, newRefreshTokenData.Token, databaseToken.Type);
      }

      public Task<Either<LogoutError, LogoutResponse>> LogoutAsync(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
      {
         return from userId in ParseUserId(claimsPrincipal).ToEither(LogoutError.InvalidToken).AsTask()
                from tokenId in ParseRefreshTokenId(claimsPrincipal).ToEither(LogoutError.InvalidToken).AsTask()
                from databaseToken in FetchUserTokenAsync(tokenId, cancellationToken).ToEitherAsync(LogoutError.InvalidToken)
                let databaseTokenDeleted = DeleteUserToken(databaseToken)
                from entriesModified in Either<LogoutError, int>.FromRightAsync(SaveChangesAsync(cancellationToken))
                select new LogoutResponse();
      }

      public Task<Either<UpdateContactInfoError, UpdateContactInfoResponse>> UpdateUserContactInfoAsync(Guid userId, UpdateContactInfoRequest request, CancellationToken cancellationToken)
      {
         return from user in FetchUserAsync(userId, cancellationToken).ToEitherAsync(UpdateContactInfoError.UserNotFound)
                from currentPassword in AuthenticationPassword.TryFrom(request.CurrentPassword, out var validPassword)
                  ? Either<UpdateContactInfoError, AuthenticationPassword>.FromRight(validPassword).AsTask()
                  : Either<UpdateContactInfoError, AuthenticationPassword>.FromLeft(UpdateContactInfoError.InvalidPassword).AsTask()
                from passwordVerified in VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt)
                  ? Either<UpdateContactInfoError, Unit>.FromRight(Unit.Default).AsTask()
                  : Either<UpdateContactInfoError, Unit>.FromLeft(UpdateContactInfoError.InvalidPassword).AsTask()
                from emailAddress in EmailAddress.TryFrom(request.EmailAddress, out var validEmailAddress)
                  ? Either<UpdateContactInfoError, EmailAddress>.FromRight(validEmailAddress).AsTask()
                  : Either<UpdateContactInfoError, EmailAddress>.FromLeft(UpdateContactInfoError.InvalidEmailAddress).AsTask()
                from isEmailAddressAvailable in Either<UpdateContactInfoError, bool>.FromRightAsync(VerifyEmailIsAddressAvailable(emailAddress, cancellationToken))
                from emailAddressAvailabilityCheck in isEmailAddressAvailable
                  ? Either<UpdateContactInfoError, Unit>.FromRight(Unit.Default).AsTask()
                  : Either<UpdateContactInfoError, Unit>.FromLeft(UpdateContactInfoError.EmailAddressUnavailable).AsTask()
                let emailAddressChanged = string.IsNullOrEmpty(user.EmailAddress) || user.EmailAddress.ToLower() != emailAddress.Value.ToLower()
                let _ = user.EmailAddress = request.EmailAddress
                from notificationSettingsReset in Either<UpdateContactInfoError, Unit>.FromRightAsync(ResetUserNotificationSettings(user.Id, cancellationToken))
                from pendingEmailVerificationDeleted in Either<UpdateContactInfoError, Unit>.FromRightAsync(DeleteUserEmailVerificationEntityAsync(user.Id, cancellationToken))
                from entriesModified in Either<UpdateContactInfoError, int>.FromRightAsync(SaveChangesAsync(cancellationToken))
                let jobId = emailAddressChanged
                  ? EnqueueEmailAddressVerificationEmailDelivery(user.Id)
                  : string.Empty
                select new UpdateContactInfoResponse();
      }

      private Either<LoginError, ValidLoginRequest> ValidateLoginRequest(LoginRequest request)
      {
         if (!Username.TryFrom(request.Username, out var validUsername))
         {
            return LoginError.InvalidUsername;
         }

         if (!AuthenticationPassword.TryFrom(request.Password, out var validAuthenticationPassword))
         {
            return LoginError.InvalidPassword;
         }

         if (!_refreshTokenProviderMap.ContainsKey(request.RefreshTokenType))
         {
            return LoginError.InvalidTokenTypeRequested;
         }

         return new ValidLoginRequest(validUsername, validAuthenticationPassword, request.RefreshTokenType);
      }

      private static Either<RegistrationError, ValidRegistrationRequest> ValidateRegistrationRequest(RegistrationRequest request)
      {
         if (!Username.TryFrom(request.Username, out var validUsername))
         {
            return RegistrationError.InvalidUsername;
         }

         if (!AuthenticationPassword.TryFrom(request.Password, out var validAuthenticationPassword))
         {
            return RegistrationError.InvalidPassword;
         }

         bool isPossibleEmailAddress = !string.IsNullOrEmpty(request.EmailAddress);
         Maybe<EmailAddress> validatedEmailAddress = EmailAddress.TryFrom(request.EmailAddress, out var validEmailAddressOrNull)
            ? validEmailAddressOrNull
            : Maybe<EmailAddress>.None;

         if (isPossibleEmailAddress && validatedEmailAddress.IsNone)
         {
            return RegistrationError.InvalidEmailAddress;
         }

         return new ValidRegistrationRequest(validUsername, validAuthenticationPassword, validatedEmailAddress);
      }

      private Task<bool> VerifyUsernameIsAvailableAsync(Username username, CancellationToken cancellationToken)
      {
         return _context.Users.IsUsernameAvailableAsync(username, cancellationToken);
      }

      private Task<bool> VerifyEmailIsAddressAvailable(EmailAddress emailAddress, CancellationToken cancellationToken)
      {
         return _context.Users.IsEmailAddressAvailableAsync(emailAddress, cancellationToken);
      }

      private static Maybe<Unit> ValidateUserToken(UserTokenEntity token, Guid userId)
      {
         bool isTokenValid = token.Owner == userId
            && token.Expiration >= DateTime.UtcNow;

         return isTokenValid
            ? Unit.Default
            : Maybe<Unit>.None;
      }

      private UserEntity InsertNewUser(Username username, Maybe<EmailAddress> emailAddress, byte[] passwordSalt, byte[] passwordHash)
      {
         UserEntity user = new UserEntity(Guid.NewGuid(), username, emailAddress, passwordHash, passwordSalt, false, DateTime.UtcNow, DateTime.MinValue);
         user.Profile = new UserProfileEntity(user.Id, string.Empty, string.Empty, string.Empty);
         user.PrivacySetting = new UserPrivacySettingEntity(user.Id, true, UserVisibilityLevel.Everyone, UserItemTransferPermission.Everyone, UserItemTransferPermission.Everyone);
         user.NotificationSetting = new UserNotificationSettingEntity(user.Id, false, false);

         _context.Users.Add(user);
         return user;
      }

      private Task<Maybe<UserEntity>> FetchUserAsync(Guid userId, CancellationToken cancellationToken)
      {
         return Maybe<UserEntity>.FromAsync(_context.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken));
      }

      private Task<Either<LoginError, UserEntity>> FetchUserAsync(ValidLoginRequest request, CancellationToken cancellationToken)
      {
         return Either<LoginError, UserEntity>.FromRightAsync(
            rightAsync: _context.Users
               .FirstOrDefaultAsync(x => x.Username.ToLower() == request.Username.Value, cancellationToken),
            left: LoginError.InvalidUsername);
      }

      private bool VerifyPassword(AuthenticationPassword password, byte[] existingPasswordHash, byte[] passwordSalt)
      {
         return _passwordHashService.VerifySecurePasswordHash(password, existingPasswordHash, passwordSalt);
      }

      private static Unit UpdateLastLoginTime(UserEntity user)
      {
         user.LastLogin = DateTime.UtcNow;
         return Unit.Default;
      }

      private Maybe<Guid> ParseUserId(ClaimsPrincipal claimsPrincipal)
      {
         return _tokenService.TryParseUserId(claimsPrincipal);
      }

      private Maybe<Guid> ParseRefreshTokenId(ClaimsPrincipal claimsPrincipal)
      {
         return _tokenService.TryParseTokenId(claimsPrincipal);
      }

      private RefreshTokenData MakeRefreshToken(Guid userId, TokenType tokenType)
      {
         return _refreshTokenProviderMap[tokenType].Invoke(userId);
      }

      private Task<Maybe<UserTokenEntity>> FetchUserTokenAsync(Guid tokenId, CancellationToken cancellationToken)
      {
         return Maybe<UserTokenEntity>.FromAsync(_context.UserTokens
            .FindAsync(new object[] { tokenId }, cancellationToken).AsTask());
      }

      private Unit StoreRefreshToken(Guid userId, TokenType tokenType, RefreshTokenData tokenData, string deviceDescription)
      {
         UserTokenEntity tokenEntity = new(tokenData.TokenId, userId, deviceDescription, tokenType, tokenData.Created, tokenData.Expiration);
         _context.UserTokens.Add(tokenEntity);
         return Unit.Default;
      }

      private Unit DeleteUserToken(UserTokenEntity token)
      {
         _context.UserTokens.Remove(token);
         return Unit.Default;
      }

      private string EnqueueEmailAddressVerificationEmailDelivery(Guid userId)
      {
         return BackgroundJob.Enqueue(() => _hangfireBackgroundService.SendEmailVerificationAsync(userId, CancellationToken.None));
      }

      private string ScheduleRefreshTokenDeletion(Guid tokenId, DateTime tokenExpiration)
      {
         return BackgroundJob.Schedule(() => _hangfireBackgroundService.DeleteUserTokenAsync(tokenId, CancellationToken.None), tokenExpiration - DateTime.UtcNow);
      }

      private string MakeAuthenticationToken(Guid userId)
      {
         return _tokenService.NewAuthenticationToken(userId);
      }

      private async Task<Unit> ResetUserNotificationSettings(Guid userId, CancellationToken cancellationToken)
      {
         UserNotificationSettingEntity foundEntity = await _context.UserNotificationSettings
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (foundEntity is not null)
         {
            foundEntity.EnableTransferNotifications = false;
            foundEntity.EmailNotifications = false;
         }

         return Unit.Default;
      }

      private async Task<Unit> DeleteUserEmailVerificationEntityAsync(Guid userId, CancellationToken cancellationToken)
      {
         UserEmailVerificationEntity foundEntity = await _context.UserEmailVerifications
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (foundEntity is not null)
         {
            _context.UserEmailVerifications.Remove(foundEntity);
         }

         return Unit.Default;
      }

      private Task<int> SaveChangesAsync(CancellationToken cancellationToken)
      {
         return _context.SaveChangesAsync(cancellationToken);
      }

      private record ValidLoginRequest
      {
         public Username Username { get; init; }
         public AuthenticationPassword Password { get; init; }
         public TokenType RefreshTokenType { get; init; }

         public ValidLoginRequest(Username username, AuthenticationPassword password, TokenType refreshTokenType)
         {
            Username = username;
            Password = password;
            RefreshTokenType = refreshTokenType;
         }
      }

      private record ValidRegistrationRequest
      {
         public Username Username { get; init; }
         public AuthenticationPassword Password { get; init; }
         public Maybe<EmailAddress> EmailAddress { get; init; }

         public ValidRegistrationRequest(Username username, AuthenticationPassword password, Maybe<EmailAddress> emailAddress)
         {
            Username = username;
            Password = password;
            EmailAddress = emailAddress;
         }
      }
   }
}
