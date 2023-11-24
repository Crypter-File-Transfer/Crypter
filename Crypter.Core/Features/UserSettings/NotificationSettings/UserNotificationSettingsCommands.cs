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
using System.Linq;
using System.Threading.Tasks;
using Crypter.Core.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;
using Contracts = Crypter.Common.Contracts.Features.UserSettings.NotificationSettings;

namespace Crypter.Core.Features.UserSettings.NotificationSettings;

internal static class UserNotificationSettingsCommands
{
    internal static async Task<Either<Contracts.UpdateNotificationSettingsError, Contracts.NotificationSettings>>
        UpdateNotificationSettingsAsync(DataContext dataContext, Guid userId, Contracts.NotificationSettings request)
    {
        var userData = await dataContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new { x.EmailVerified, x.NotificationSetting })
            .FirstOrDefaultAsync();

        if (userData is null)
        {
            return Contracts.UpdateNotificationSettingsError.UnknownError;
        }

        if (!userData.EmailVerified && request.EmailNotifications)
        {
            return Contracts.UpdateNotificationSettingsError.EmailAddressNotVerified;
        }

        if (userData.NotificationSetting is null)
        {
            UserNotificationSettingEntity newNotificationSettings =
                new UserNotificationSettingEntity(userId, request.NotifyOnTransferReceived, request.EmailNotifications);
            dataContext.UserNotificationSettings.Add(newNotificationSettings);
        }
        else
        {
            userData.NotificationSetting.EmailNotifications = request.EmailNotifications;
            userData.NotificationSetting.EnableTransferNotifications = request.NotifyOnTransferReceived;
        }

        await dataContext.SaveChangesAsync();

        return await UserNotificationSettingsQueries.GetUserNotificationSettingsAsync(dataContext, userId)
            .ToEitherAsync(Contracts.UpdateNotificationSettingsError.UnknownError);
    }

    internal static async Task ResetUserNotificationSettingsAsync(DataContext dataContext, Guid userId,
        bool saveChanges)
    {
        UserNotificationSettingEntity foundEntity = await dataContext.UserNotificationSettings
            .FirstOrDefaultAsync(x => x.Owner == userId);

        if (foundEntity is not null)
        {
            foundEntity.EnableTransferNotifications = false;
            foundEntity.EmailNotifications = false;
        }

        if (saveChanges)
        {
            await dataContext.SaveChangesAsync();
        }
    }
}
