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
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.DataContextExtensions;
using Crypter.Core.Features.UserEmailVerification;
using Crypter.Core.Features.UserSettings.NotificationSettings;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;
using Contracts = Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;

namespace Crypter.Core.Features.UserSettings.ContactInfoSettings;

internal static class UserContactInfoSettingsCommands
{
    internal static async Task<Either<Contracts.UpdateContactInfoSettingsError, Contracts.ContactInfoSettings>>
        UpdateContactInfoSettingsAsync(DataContext dataContext, IPasswordHashService passwordHashService,
            ServerPasswordSettings serverPasswordSettings, Guid userId,
            Contracts.UpdateContactInfoSettingsRequest request)
    {
        if (!AuthenticationPassword.TryFrom(request.CurrentPassword, out AuthenticationPassword _))
        {
            return Contracts.UpdateContactInfoSettingsError.InvalidPassword;
        }
        
        bool noEmailAddressProvided = string.IsNullOrEmpty(request.EmailAddress);
        bool validEmailAddressProvided = EmailAddress.TryFrom(request.EmailAddress, out EmailAddress validEmailAddress);
        bool validEmailAddressOption = noEmailAddressProvided || validEmailAddressProvided;
        if (!validEmailAddressOption)
        {
            return Contracts.UpdateContactInfoSettingsError.InvalidEmailAddress;
        }

        Maybe<EmailAddress> newEmailAddress = validEmailAddressProvided
            ? validEmailAddress
            : Maybe<EmailAddress>.None;

        UserEntity user = await dataContext.Users
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return Contracts.UpdateContactInfoSettingsError.UserNotFound;
        }

        if (user.ClientPasswordVersion != serverPasswordSettings.ClientVersion
            || user.ServerPasswordVersion != passwordHashService.LatestServerPasswordVersion)
        {
            return Contracts.UpdateContactInfoSettingsError.PasswordNeedsMigration;
        }

        bool correctPasswordProvided = passwordHashService.VerifySecurePasswordHash(request.CurrentPassword,
            user.PasswordHash, user.PasswordSalt, passwordHashService.LatestServerPasswordVersion);
        if (!correctPasswordProvided)
        {
            return Contracts.UpdateContactInfoSettingsError.InvalidPassword;
        }

        if (user.EmailAddress != newEmailAddress.Match(() => string.Empty, x => x.Value))
        {
            bool isEmailAddressAvailableForUser = await newEmailAddress.MatchAsync(
                () => true,
                async _ => await dataContext.Users.IsEmailAddressAvailableAsync(validEmailAddress));

            if (!isEmailAddressAvailableForUser)
            {
                return Contracts.UpdateContactInfoSettingsError.EmailAddressUnavailable;
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
            .ToEitherAsync(Contracts.UpdateContactInfoSettingsError.UnknownError);
    }
}
