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
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class FileTransferItemService : IBaseTransferService<IFileTransferItem>
   {
      private readonly DataContext Context;

      public FileTransferItemService(DataContext context)
      {
         Context = context;
      }

      public async Task InsertAsync(IFileTransferItem item, CancellationToken cancellationToken)
      {
         Context.FileTransfer.Add((FileTransfer)item);
         await Context.SaveChangesAsync(cancellationToken);
      }

      public async Task<IFileTransferItem> ReadAsync(Guid id, CancellationToken cancellationToken)
      {
         return await Context.FileTransfer
             .FindAsync(new object[] { id }, cancellationToken);
      }

      public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"FileTransfer\" WHERE \"FileTransfer\".\"Id\" = {0}", new object[] { id }, cancellationToken);
      }

      public async Task<IEnumerable<IFileTransferItem>> FindBySenderAsync(Guid senderId, CancellationToken cancellationToken)
      {
         return await Context.FileTransfer
             .Where(x => x.Sender == senderId)
             .ToListAsync(cancellationToken);
      }

      public async Task<IEnumerable<IFileTransferItem>> FindByRecipientAsync(Guid recipientId, CancellationToken cancellationToken)
      {
         return await Context.FileTransfer
             .Where(x => x.Recipient == recipientId)
             .ToListAsync(cancellationToken);
      }

      public async Task<IEnumerable<IFileTransferItem>> FindExpiredAsync(CancellationToken cancellationToken)
      {
         return await Context.FileTransfer
             .Where(x => x.Expiration < DateTime.UtcNow)
             .ToListAsync(cancellationToken);
      }

      public async Task<long> GetAggregateSizeAsync(CancellationToken cancellationToken)
      {
         return await Context.FileTransfer
             .SumAsync(x => x.Size, cancellationToken);
      }
   }
}
