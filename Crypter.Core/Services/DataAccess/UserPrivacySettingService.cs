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
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserPrivacySettingService : IUserPrivacySettingService
   {
      private readonly DataContext _context;

      public UserPrivacySettingService(DataContext context)
      {
         _context = context;
      }

      public async Task<bool> UpsertAsync(Guid userId, bool allowKeyExchangeRequests, UserVisibilityLevel visibilityLevel, UserItemTransferPermission receiveFilesPermission, UserItemTransferPermission receiveMessagesPermission, CancellationToken cancellationToken)
      {
         var userPrivacySettings = await ReadAsync(userId, cancellationToken);
         if (userPrivacySettings == null)
         {
            var newPrivacySettings = new UserPrivacySettingEntity(userId, allowKeyExchangeRequests, visibilityLevel, receiveFilesPermission, receiveMessagesPermission);
            _context.UserPrivacySettings.Add(newPrivacySettings);
         }
         else
         {
            userPrivacySettings.AllowKeyExchangeRequests = allowKeyExchangeRequests;
            userPrivacySettings.Visibility = visibilityLevel;
            userPrivacySettings.ReceiveFiles = receiveFilesPermission;
            userPrivacySettings.ReceiveMessages = receiveMessagesPermission;
         }

         await _context.SaveChangesAsync(cancellationToken);
         return true;
      }

      public async Task<UserPrivacySettingEntity> ReadAsync(Guid userId, CancellationToken cancellationToken)
      {
         return await _context.UserPrivacySettings.FindAsync(new object[] { userId }, cancellationToken);
      }
   }
}
