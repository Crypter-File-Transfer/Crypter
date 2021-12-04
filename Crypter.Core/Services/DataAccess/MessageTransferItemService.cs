/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class MessageTransferItemService : IBaseTransferService<MessageTransfer>
   {
      private readonly DataContext Context;

      public MessageTransferItemService(DataContext context)
      {
         Context = context;
      }

      public async Task InsertAsync(MessageTransfer item)
      {
         Context.MessageTransfer.Add(item);
         await Context.SaveChangesAsync();
      }

      public async Task<MessageTransfer> ReadAsync(Guid id)
      {
         return await Context.MessageTransfer
             .FindAsync(id);
      }

      public async Task DeleteAsync(Guid id)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"MessageTransfer\" WHERE \"MessageTransfer\".\"Id\" = {0}", id);
      }

      public async Task<IEnumerable<MessageTransfer>> FindBySenderAsync(Guid ownerId)
      {
         return await Context.MessageTransfer
             .Where(x => x.Sender == ownerId)
             .ToListAsync();
      }

      public async Task<IEnumerable<MessageTransfer>> FindByRecipientAsync(Guid recipientId)
      {
         return await Context.MessageTransfer
             .Where(x => x.Recipient == recipientId)
             .ToListAsync();
      }

      public async Task<IEnumerable<MessageTransfer>> FindExpiredAsync()
      {
         return await Context.MessageTransfer
             .Where(x => x.Expiration < DateTime.UtcNow)
             .ToListAsync();
      }

      public async Task<long> GetAggregateSizeAsync()
      {
         return await Context.MessageTransfer
             .SumAsync(x => x.Size);
      }
   }
}
