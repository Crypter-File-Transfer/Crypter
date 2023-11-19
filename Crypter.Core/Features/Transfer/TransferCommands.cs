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
using System.Threading.Tasks;
using Crypter.Common.Enums;
using Crypter.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer;

internal static class TransferCommands
{
   internal static async Task DeleteTransferAsync(DataContext dataContext, ITransferRepository transferRepository, Guid itemId, TransferItemType itemType, TransferUserType userType, bool deleteFromTransferStorage)
   {
      bool entityFound = false;

      switch (itemType)
      {
         case TransferItemType.Message:
            switch (userType)
            {
               case TransferUserType.Anonymous:
                  var anonymousEntity = await dataContext.AnonymousMessageTransfers.FirstOrDefaultAsync(x => x.Id == itemId);
                  if (anonymousEntity is not null)
                  {
                     dataContext.AnonymousMessageTransfers.Remove(anonymousEntity);
                     entityFound = true;
                  }
                  break;
               case TransferUserType.User:
                  var userEntity = await dataContext.UserMessageTransfers.FirstOrDefaultAsync(x => x.Id == itemId);
                  if (userEntity is not null)
                  {
                     dataContext.UserMessageTransfers.Remove(userEntity);
                     entityFound = true;
                  }
                  break;
            }
            break;
         case TransferItemType.File:
            switch (userType)
            {
               case TransferUserType.Anonymous:
                  var anonymousEntity = await dataContext.AnonymousFileTransfers.FirstOrDefaultAsync(x => x.Id == itemId);
                  if (anonymousEntity is not null)
                  {
                     dataContext.AnonymousFileTransfers.Remove(anonymousEntity);
                     entityFound = true;
                  }
                  break;
               case TransferUserType.User:
                  var userEntity = await dataContext.UserFileTransfers.FirstOrDefaultAsync(x => x.Id == itemId);
                  if (userEntity is not null)
                  {
                     dataContext.UserFileTransfers.Remove(userEntity);
                     entityFound = true;
                  }
                  break;
            }
            break;
      }

      if (entityFound)
      {
         await dataContext.SaveChangesAsync();
      }

      if (deleteFromTransferStorage)
      {
         transferRepository.DeleteTransfer(itemId, itemType, userType);
      }
   }
}