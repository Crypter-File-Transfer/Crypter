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
using Crypter.Contracts.Features.Contacts;
using Crypter.Contracts.Features.Users;
using Crypter.Core.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Crypter.Core.Extensions
{
   public static class LinqUserExpressions
   {
      public static Expression<Func<UserEntity, bool>> UserPrivacyAllowsVisitor(Guid? visitorId)
      {
         return (x) => x.Id == visitorId
            || x.PrivacySetting.Visibility == UserVisibilityLevel.Everyone
            || (x.PrivacySetting.Visibility == UserVisibilityLevel.Authenticated && visitorId != null)
            || (x.PrivacySetting.Visibility == UserVisibilityLevel.Contacts && x.Contacts.Any(y => y.ContactId == visitorId));
      }

      public static Expression<Func<UserEntity, UserContactDTO>> ToUserContactDTO(Guid? visitorId)
      {
         return (x) => x.Id == visitorId
            || x.PrivacySetting.Visibility == UserVisibilityLevel.Everyone
            || (x.PrivacySetting.Visibility == UserVisibilityLevel.Authenticated && visitorId != null)
            || (x.PrivacySetting.Visibility == UserVisibilityLevel.Contacts && x.Contacts.Any(y => y.ContactId == visitorId))
            ? new UserContactDTO(x.Username, x.Profile.Alias)
            : new UserContactDTO("{ Private }", string.Empty);
      }

      public static Expression<Func<UserEntity, bool>> UserProfileIsComplete()
      {
         return (x) => x.Profile != null
            && x.X25519KeyPair != null
            && x.PrivacySetting != null;
      }

      public static Expression<Func<UserEntity, UserProfileDTO>> ToUserProfileDTOForVisitor(Guid? visitorId)
      {
         return (x) => new UserProfileDTO(
            x.Username,
            x.Profile.Alias,
            x.Profile.About,
            x.PrivacySetting.AllowKeyExchangeRequests,
            x.Id == visitorId
               || x.PrivacySetting.ReceiveMessages == UserItemTransferPermission.Everyone
               || (x.PrivacySetting.ReceiveMessages == UserItemTransferPermission.Authenticated && visitorId != null)
               || (x.PrivacySetting.ReceiveMessages == UserItemTransferPermission.Contacts && x.Contacts.Any(y => y.ContactId == visitorId)),
            x.Id == visitorId
               || x.PrivacySetting.ReceiveFiles == UserItemTransferPermission.Everyone
               || (x.PrivacySetting.ReceiveFiles == UserItemTransferPermission.Authenticated && visitorId != null)
               || (x.PrivacySetting.ReceiveFiles == UserItemTransferPermission.Contacts && x.Contacts.Any(y => y.ContactId == visitorId)),
            x.X25519KeyPair.PublicKey);
      }

      public static Expression<Func<UserEntity, bool>> UserReceivesEmailNotifications()
      {
         return (x) => x.EmailVerified
            && x.NotificationSetting != null
            && x.NotificationSetting.EnableTransferNotifications
            && x.NotificationSetting.EmailNotifications;
      }

      public static Expression<Func<T, bool>> Inverse<T>(this Expression<Func<T, bool>> e)
      {
         return Expression.Lambda<Func<T, bool>>(Expression.Not(e.Body), e.Parameters[0]);
      }
   }
}
