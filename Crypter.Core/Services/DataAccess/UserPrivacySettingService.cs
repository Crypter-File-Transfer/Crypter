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

using Crypter.Contracts.Common.Enum;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserPrivacySettingService : IUserPrivacySettingService
   {
      private readonly DataContext Context;

      public UserPrivacySettingService(DataContext context)
      {
         Context = context;
      }

      public async Task<bool> UpsertAsync(Guid userId, bool allowKeyExchangeRequests, UserVisibilityLevel visibilityLevel, UserItemTransferPermission receiveFilesPermission, UserItemTransferPermission receiveMessagesPermission, CancellationToken cancellationToken)
      {
         var userPrivacySettings = await ReadAsync(userId, cancellationToken);
         if (userPrivacySettings == null)
         {
            var newPrivacySettings = new UserPrivacySetting(userId, allowKeyExchangeRequests, visibilityLevel, receiveFilesPermission, receiveMessagesPermission);
            Context.UserPrivacySettings.Add(newPrivacySettings);
         }
         else
         {
            userPrivacySettings.AllowKeyExchangeRequests = allowKeyExchangeRequests;
            userPrivacySettings.Visibility = visibilityLevel;
            userPrivacySettings.ReceiveFiles = receiveFilesPermission;
            userPrivacySettings.ReceiveMessages = receiveMessagesPermission;
         }

         await Context.SaveChangesAsync(cancellationToken);
         return true;
      }

      public async Task<IUserPrivacySetting> ReadAsync(Guid userId, CancellationToken cancellationToken)
      {
         return await Context.UserPrivacySettings.FindAsync(new object[] { userId }, cancellationToken);
      }

      public async Task<bool> IsUserViewableByPartyAsync(Guid userId, Guid otherPartyId, CancellationToken cancellationToken)
      {
         if (userId.Equals(otherPartyId))
         {
            return true;
         }

         var userVisibility = (await Context.UserPrivacySettings
            .Where(x => x.Owner == userId)
            .FirstOrDefaultAsync(cancellationToken))
            .Visibility;

         return userVisibility switch
         {
            UserVisibilityLevel.None => false,
            UserVisibilityLevel.Contacts => false,
            UserVisibilityLevel.Authenticated => otherPartyId != Guid.Empty,
            UserVisibilityLevel.Everyone => true,
            _ => false,
         };
      }

      public async Task<bool> DoesUserAcceptMessagesFromOtherPartyAsync(Guid userId, Guid otherPartyId, CancellationToken cancellationToken)
      {
         var messageTransferPermission = (await Context.UserPrivacySettings
            .Where(x => x.Owner == userId)
            .FirstOrDefaultAsync(cancellationToken))
            .ReceiveMessages;

         return messageTransferPermission switch
         {
            UserItemTransferPermission.None => false,
            UserItemTransferPermission.ExchangedKeys => false,
            UserItemTransferPermission.Contacts => false,
            UserItemTransferPermission.Authenticated => otherPartyId != Guid.Empty,
            UserItemTransferPermission.Everyone => true,
            _ => false,
         };
      }

      public async Task<bool> DoesUserAcceptFilesFromOtherPartyAsync(Guid userId, Guid otherPartyId, CancellationToken cancellationToken)
      {
         var fileTransferPermission = (await Context.UserPrivacySettings
            .Where(x => x.Owner == userId)
            .FirstOrDefaultAsync(cancellationToken))
            .ReceiveFiles;

         return fileTransferPermission switch
         {
            UserItemTransferPermission.None => false,
            UserItemTransferPermission.ExchangedKeys => false,
            UserItemTransferPermission.Contacts => false,
            UserItemTransferPermission.Authenticated => otherPartyId != Guid.Empty,
            UserItemTransferPermission.Everyone => true,
            _ => false,
         };
      }
   }
}
