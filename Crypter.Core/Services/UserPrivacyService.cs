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
using Crypter.Core.Entities;
using Crypter.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crypter.Core.Services
{
   public class UserPrivacyService : IUserPrivacyService
   {
      public bool UserIsVisibleToVisitor(UserEntity user, Guid visitorID)
      {
         if (user.Contacts == null)
         {
            throw new InvalidOperationException("User contacts may not be null.");
         }

         if (user.PrivacySetting == null)
         {
            throw new InvalidOperationException("User privacy setting may not be null.");
         }

         if (user.Id.Equals(visitorID))
         {
            return true;
         }

         return user.PrivacySetting.Visibility switch
         {
            UserVisibilityLevel.None => false,
            UserVisibilityLevel.Contacts => user.Contacts.Any(x => x.ContactId == visitorID),
            UserVisibilityLevel.Authenticated => !visitorID.Equals(Guid.Empty),
            UserVisibilityLevel.Everyone => true,
            _ => false,
         };
      }

      public bool UserAcceptsFileTransfersFromVisitor(UserEntity user, Guid visitorId)
      {
         return UserAcceptsTransferFromVisitor(user, visitorId, user.PrivacySetting.ReceiveFiles);
      }

      public bool UserAcceptsMessageTransfersFromVisitor(UserEntity user, Guid visitorId)
      {
         return UserAcceptsTransferFromVisitor(user, visitorId, user.PrivacySetting.ReceiveMessages);
      }

      private static bool UserAcceptsTransferFromVisitor(UserEntity user, Guid visitorId, UserItemTransferPermission transferPermission)
      {
         return transferPermission switch
         {
            UserItemTransferPermission.None => false,
            UserItemTransferPermission.ExchangedKeys => false,
            UserItemTransferPermission.Contacts => VisitorIsUserContact(user.Contacts, visitorId),
            UserItemTransferPermission.Authenticated => !visitorId.Equals(Guid.Empty),
            UserItemTransferPermission.Everyone => true,
            _ => false,
         };
      }

      private static bool VisitorIsUserContact(List<UserContactEntity> contacts, Guid visitorId)
      {
         return contacts.Any(x => x.ContactId == visitorId);
      }
   }
}
