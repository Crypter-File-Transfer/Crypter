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
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;
using Crypter.Common.Contracts.Features.UserSettings.NotificationSettings;
using Crypter.Common.Contracts.Features.UserSettings.PrivacySettings;
using Crypter.Common.Contracts.Features.UserSettings.ProfileSettings;
using Crypter.DataAccess;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserSettings;

internal static class Common
{
    internal static Task<Maybe<ContactInfoSettings>> GetContactInfoSettingsAsync(
        DataContext dataContext, Guid userId, CancellationToken cancellationToken = default)
    {
        return Maybe<ContactInfoSettings>.FromNullableAsync(dataContext.Users
            .Where(x => x.Id == userId)
            .Select(x => new ContactInfoSettings(x.EmailAddress, x.EmailVerified))
            .FirstOrDefaultAsync(cancellationToken));
    }
    
    internal static Task<Maybe<PrivacySettings>> GetPrivacySettingsAsync(DataContext dataContext,
        Guid userId, CancellationToken cancellationToken = default)
    {
        return Maybe<PrivacySettings>.FromNullableAsync(dataContext.UserPrivacySettings
            .Where(x => x.Owner == userId)
            .Select(x =>
                new PrivacySettings(x.AllowKeyExchangeRequests, x.Visibility, x.ReceiveMessages,
                    x.ReceiveFiles))
            .FirstOrDefaultAsync(cancellationToken));
    }
    
    internal static Task<Maybe<ProfileSettings>> GetProfileSettingsAsync(DataContext dataContext,
        Guid userId, CancellationToken cancellationToken = default)
    {
        return Maybe<ProfileSettings>.FromNullableAsync(dataContext.UserProfiles
            .Where(x => x.Owner == userId)
            .Select(x => new ProfileSettings(x.Alias, x.About))
            .FirstOrDefaultAsync(cancellationToken));
    }
    
    internal static Task<Maybe<NotificationSettings>> GetUserNotificationSettingsAsync(
        DataContext dataContext, Guid userId, CancellationToken cancellationToken = default)
    {
        return Maybe<NotificationSettings>.FromNullableAsync(dataContext.UserNotificationSettings
            .Where(x => x.Owner == userId)
            .Select(x => new NotificationSettings(x.EmailNotifications, x.EnableTransferNotifications))
            .FirstOrDefaultAsync(cancellationToken));
    }
}
