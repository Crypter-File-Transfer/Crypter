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

using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Common.Monads;
using Crypter.Core.Entities;
using Crypter.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserService
   {
      Task<Maybe<UserEntity>> GetUserEntityAsync(Guid id, CancellationToken cancellationToken);
      Task<Maybe<UserEntity>> GetUserEntityAsync(string username, CancellationToken cancellationToken);
      Task<Maybe<GetUserProfileResponse>> GetUserProfileAsync(Maybe<Guid> userId, string username, CancellationToken cancellationToken);
      Task<UpdateProfileResponse> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
      Task<UserSettingsResponse> GetUserSettingsAsync(Guid userId, CancellationToken cancellationToken);
      Task<UpdatePrivacySettingsResponse> UpsertUserPrivacySettingsAsync(Guid userId, UpdatePrivacySettingsRequest request, CancellationToken cancellationToken);
      Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpsertUserNotificationPreferencesAsync(Guid userId, UpdateNotificationSettingsRequest request, CancellationToken cancellationToken);
      Task<List<UserSearchResult>> SearchForUsersAsync(Guid userId, string keyword, int index, int count, CancellationToken cancellationToken);
      Task<Unit> SaveUserAcknowledgementOfRecoveryKeyRisksAsync(Guid userId);
      Task DeleteUserEntityAsync(Guid id, CancellationToken cancellationToken);
      Task DeleteUserTokenEntityAsync(Guid tokenId, CancellationToken cancellationToken);
   }

   public class UserService : IUserService
   {
      private readonly DataContext _context;

      public UserService(DataContext context)
      {
         _context = context;
      }

      public async Task<Maybe<UserEntity>> GetUserEntityAsync(Guid id, CancellationToken cancellationToken)
      {
         return await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
      }

      public async Task<Maybe<UserEntity>> GetUserEntityAsync(string username, CancellationToken cancellationToken)
      {
         return await _context.Users
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
      }

      public async Task<Maybe<GetUserProfileResponse>> GetUserProfileAsync(Maybe<Guid> userId, string username, CancellationToken cancellationToken)
      {
         Guid? visitorId = userId.Match<Guid?>(
            () => null,
            x => x);

         var profileDTO = await _context.Users
            .Where(x => x.Username == username)
            .Where(LinqUserExpressions.UserProfileIsComplete())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(visitorId))
            .Select(LinqUserExpressions.ToUserProfileDTOForVisitor(visitorId))
            .FirstOrDefaultAsync(cancellationToken);

         return profileDTO is null
            ? Maybe<GetUserProfileResponse>.None
            : new GetUserProfileResponse(profileDTO);
      }

      public async Task<UpdateProfileResponse> UpdateUserProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
      {
         var userProfile = await _context.UserProfiles
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (userProfile is not null)
         {
            userProfile.About = request.About;
            userProfile.Alias = request.Alias;
            await _context.SaveChangesAsync(cancellationToken);
         }

         return new UpdateProfileResponse();
      }

      public Task<UserSettingsResponse> GetUserSettingsAsync(Guid userId, CancellationToken cancellationToken)
      {
         return _context.Users
            .Where(x => x.Id == userId)
            .Select(x => new UserSettingsResponse(
               x.Username,
               x.EmailAddress,
               x.EmailVerified,
               x.Profile.Alias,
               x.Profile.About,
               x.PrivacySetting.Visibility,
               x.PrivacySetting.AllowKeyExchangeRequests,
               x.PrivacySetting.ReceiveMessages,
               x.PrivacySetting.ReceiveFiles,
               x.NotificationSetting.EnableTransferNotifications,
               x.NotificationSetting.EmailNotifications,
               x.Created))
            .FirstOrDefaultAsync(cancellationToken);
      }

      public async Task<UpdatePrivacySettingsResponse> UpsertUserPrivacySettingsAsync(Guid userId, UpdatePrivacySettingsRequest request, CancellationToken cancellationToken)
      {
         var userPrivacySettings = await _context.UserPrivacySettings
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (userPrivacySettings is null)
         {
            var newPrivacySettings = new UserPrivacySettingEntity(userId, request.AllowKeyExchangeRequests, request.VisibilityLevel, request.FileTransferPermission, request.MessageTransferPermission);
            _context.UserPrivacySettings.Add(newPrivacySettings);
         }
         else
         {
            userPrivacySettings.AllowKeyExchangeRequests = request.AllowKeyExchangeRequests;
            userPrivacySettings.Visibility = request.VisibilityLevel;
            userPrivacySettings.ReceiveFiles = request.FileTransferPermission;
            userPrivacySettings.ReceiveMessages = request.MessageTransferPermission;
         }

         await _context.SaveChangesAsync(cancellationToken);
         return new UpdatePrivacySettingsResponse();
      }

      public async Task<Either<UpdateNotificationSettingsError, UpdateNotificationSettingsResponse>> UpsertUserNotificationPreferencesAsync(Guid userId, UpdateNotificationSettingsRequest request, CancellationToken cancellationToken)
      {
         bool userEmailVerified = await _context.Users
            .Where(x => x.Id == userId)
            .Select(x => x.EmailVerified)
            .FirstOrDefaultAsync(cancellationToken);

         if (!userEmailVerified
            && (request.EnableTransferNotifications || request.EmailNotifications))
         {
            return UpdateNotificationSettingsError.EmailAddressNotVerified;
         }

         bool enableNotifications = request.EnableTransferNotifications && request.EmailNotifications;

         var userNotificationPreferences = await _context.UserNotificationSettings
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (userNotificationPreferences is null)
         {
            var newNotificationPreferences = new UserNotificationSettingEntity(userId, enableNotifications, enableNotifications);
            _context.UserNotificationSettings.Add(newNotificationPreferences);
         }
         else
         {
            userNotificationPreferences.EnableTransferNotifications = enableNotifications;
            userNotificationPreferences.EmailNotifications = enableNotifications;
         }

         await _context.SaveChangesAsync(cancellationToken);
         return new UpdateNotificationSettingsResponse();
      }

      public Task<List<UserSearchResult>> SearchForUsersAsync(Guid userId, string keyword, int index, int count, CancellationToken cancellationToken)
      {
         string lowerKeyword = keyword.ToLower();

         return _context.Users
            .Where(x => x.Username.StartsWith(lowerKeyword)
               || x.Profile.Alias.ToLower().StartsWith(lowerKeyword))
            .Where(LinqUserExpressions.UserProfileIsComplete())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(userId))
            .OrderBy(x => x.Username)
            .Skip(index)
            .Take(count)
            .Select(x => new UserSearchResult(x.Username, x.Profile.Alias))
            .ToListAsync(cancellationToken);
      }

      public async Task<Unit> SaveUserAcknowledgementOfRecoveryKeyRisksAsync(Guid userId)
      {
         UserConsentEntity newConsent = new UserConsentEntity(userId, ConsentType.RecoveryKeyRisks, DateTime.UtcNow);
         _context.UserConsents.Add(newConsent);
         await _context.SaveChangesAsync();
         return Unit.Default;
      }

      public async Task DeleteUserEntityAsync(Guid id, CancellationToken cancellationToken)
      {
         var user = await GetUserEntityAsync(id, cancellationToken);
         await user.IfSomeAsync(async x =>
         {
            _context.Users.Remove(x);
            await _context.SaveChangesAsync(cancellationToken);
         });
      }

      public async Task DeleteUserTokenEntityAsync(Guid tokenId, CancellationToken cancellationToken)
      {
         UserTokenEntity foundToken = await _context.UserTokens
            .FirstOrDefaultAsync(x => x.Id == tokenId, cancellationToken);

         if (foundToken is not null)
         {
            _context.UserTokens.Remove(foundToken);
            await _context.SaveChangesAsync(cancellationToken);
         }
      }
   }
}
