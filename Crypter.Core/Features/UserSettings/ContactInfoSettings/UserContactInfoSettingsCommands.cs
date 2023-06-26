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

using Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;
using Crypter.Common.Primitives;
using Crypter.Core.DataContextExtensions;
using Crypter.Core.Entities;
using Crypter.Core.Features.UserAuthentication;
using Crypter.Core.Features.UserEmailVerification;
using Crypter.Core.Features.UserSettings.NotificationSettings;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EasyMonads;
using Contracts = Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;

namespace Crypter.Core.Features.UserSettings.ContactInfoSettings
{
   internal static class UserContactInfoSettingsCommands
   {
      internal static async Task<Either<UpdateContactInfoSettingsError, Contracts.ContactInfoSettings>> UpdateContactInfoSettingsAsync(DataContext dataContext, IPasswordHashService passwordHashService, ServerPasswordSettings serverPasswordSettings, Guid userId, UpdateContactInfoSettingsRequest request)
      {
         if (!UserAuthenticationValidators.ValidatePassword(request.CurrentPassword))
         {
            return UpdateContactInfoSettingsError.InvalidPassword;
         }

         bool noEmailAddressProvided = string.IsNullOrEmpty(request.EmailAddress);
         bool validEmailAddressProvided = EmailAddress.TryFrom(request.EmailAddress, out EmailAddress validEmailAddress);
         bool validEmailAddressOption = noEmailAddressProvided || validEmailAddressProvided;
         if (!validEmailAddressOption)
         {
            return UpdateContactInfoSettingsError.InvalidEmailAddress;
         }

         Maybe<EmailAddress> newEmailAddress = validEmailAddressProvided
            ? validEmailAddress
            : Maybe<EmailAddress>.None;

         UserEntity user = await dataContext.Users
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

         if (user is null)
         {
            return UpdateContactInfoSettingsError.UserNotFound;
         }

         if (user.ClientPasswordVersion != serverPasswordSettings.ClientVersion
            || user.ServerPasswordVersion != passwordHashService.LatestServerPasswordVersion)
         {
            return UpdateContactInfoSettingsError.PasswordNeedsMigration;
         }

         bool correctPasswordProvided = passwordHashService.VerifySecurePasswordHash(request.CurrentPassword, user.PasswordHash, user.PasswordSalt, passwordHashService.LatestServerPasswordVersion);
         if (!correctPasswordProvided)
         {
            return UpdateContactInfoSettingsError.InvalidPassword;
         }

         if (user.EmailAddress != newEmailAddress.Match(() => string.Empty, x => x.Value))
         {
            bool isEmailAddressAvailableForUser = await newEmailAddress.MatchAsync(
               () => true,
               async x => await dataContext.Users.IsEmailAddressAvailableAsync(validEmailAddress));

            if (!isEmailAddressAvailableForUser)
            {
               return UpdateContactInfoSettingsError.EmailAddressUnavailable;
            }

            user.EmailAddress = newEmailAddress.Match(
               () => string.Empty,
               x => x.Value);
            user.EmailVerified = false;

            await UserEmailVerificationCommands.DeleteUserEmailVerificationEntity(dataContext, userId, false);

            if (newEmailAddress.IsNone)
            {
               await UserNotificationSettingsCommands.ResetUserNotificationSettingsAsync(dataContext, userId, false);
            }

            await dataContext.SaveChangesAsync();
         }

         return await UserContactInfoSettingsQueries.GetContactInfoSettingsAsync(dataContext, userId)
            .ToEitherAsync(UpdateContactInfoSettingsError.UnknownError);
      }
   }
}
