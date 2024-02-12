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
using System.Linq.Expressions;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Common.Enums;
using Crypter.DataAccess.Entities;

namespace Crypter.Core.LinqExpressions;

public static class LinqUserExpressions
{
    public static Expression<Func<UserEntity, bool>> UserPrivacyAllowsVisitor(Guid? visitorId)
    {
        return x => x.Id == visitorId
                      || x.PrivacySetting!.Visibility == UserVisibilityLevel.Everyone
                      || (x.PrivacySetting!.Visibility == UserVisibilityLevel.Authenticated && visitorId != null)
                      || (x.PrivacySetting!.Visibility == UserVisibilityLevel.Contacts &&
                          x.Contacts!.Any(y => y.ContactId == visitorId));
    }
    
    public static Expression<Func<UserEntity, bool>> UserProfileIsComplete()
    {
        return x => x.Profile != null
                      && x.KeyPair != null
                      && x.PrivacySetting != null;
    }

    /// <summary>
    /// Project a UserEntity to a UserProfile model.
    ///
    /// The query should first be filtered through 'UserProfileIsComplete' prior to ensure all required properties
    /// are present.
    /// </summary>
    /// <param name="visitorId"></param>
    /// <returns></returns>
    public static Expression<Func<UserEntity, UserProfile>> ToUserProfileForVisitor(Guid? visitorId)
    {
        return x => new UserProfile(
            x.Username,
            x.Profile!.Alias,
            x.Profile!.About,
            x.PrivacySetting!.AllowKeyExchangeRequests,
            x.Id == visitorId
            || x.PrivacySetting!.ReceiveMessages == UserItemTransferPermission.Everyone
            || (x.PrivacySetting!.ReceiveMessages == UserItemTransferPermission.Authenticated && visitorId != null)
            || (x.PrivacySetting!.ReceiveMessages == UserItemTransferPermission.Contacts &&
                x.Contacts!.Any(y => y.ContactId == visitorId)),
            x.Id == visitorId
            || x.PrivacySetting!.ReceiveFiles == UserItemTransferPermission.Everyone
            || (x.PrivacySetting!.ReceiveFiles == UserItemTransferPermission.Authenticated && visitorId != null)
            || (x.PrivacySetting!.ReceiveFiles == UserItemTransferPermission.Contacts &&
                x.Contacts!.Any(y => y.ContactId == visitorId)),
            x.KeyPair!.PublicKey,
            x.EmailVerified);
    }

    public static Expression<Func<UserEntity?, bool>> UserReceivesEmailNotifications()
    {
        return x => x != null
                      && x.EmailVerified
                      && x.NotificationSetting != null
                      && x.NotificationSetting.EnableTransferNotifications
                      && x.NotificationSetting.EmailNotifications;
    }

    public static Expression<Func<T, bool>> Inverse<T>(this Expression<Func<T, bool>> e)
    {
        return Expression.Lambda<Func<T, bool>>(Expression.Not(e.Body), e.Parameters[0]);
    }
}
