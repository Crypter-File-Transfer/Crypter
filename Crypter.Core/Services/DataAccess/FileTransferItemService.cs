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

using Crypter.Core.Entities;
using Crypter.Core.Entities.Interfaces;
using Crypter.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class FileTransferItemService : IBaseTransferService<IFileTransfer>
   {
      private readonly DataContext Context;

      public FileTransferItemService(DataContext context)
      {
         Context = context;
      }

      public async Task InsertAsync(IFileTransfer item, CancellationToken cancellationToken)
      {
         Context.FileTransfers.Add((FileTransferEntity)item);
         await Context.SaveChangesAsync(cancellationToken);
      }

      public async Task<IFileTransfer> ReadAsync(Guid id, CancellationToken cancellationToken)
      {
         return await Context.FileTransfers
             .FindAsync(new object[] { id }, cancellationToken);
      }

      public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"FileTransfer\" WHERE \"FileTransfer\".\"Id\" = {0}", new object[] { id }, cancellationToken);
      }

      public async Task<IEnumerable<IFileTransfer>> FindBySenderAsync(Guid senderId, CancellationToken cancellationToken)
      {
         return await Context.FileTransfers
             .Where(x => x.Sender == senderId)
             .ToListAsync(cancellationToken);
      }

      public async Task<IEnumerable<IFileTransfer>> FindByRecipientAsync(Guid recipientId, CancellationToken cancellationToken)
      {
         return await Context.FileTransfers
             .Where(x => x.Recipient == recipientId)
             .ToListAsync(cancellationToken);
      }

      public async Task<IEnumerable<IFileTransfer>> FindExpiredAsync(CancellationToken cancellationToken)
      {
         return await Context.FileTransfers
             .Where(x => x.Expiration < DateTime.UtcNow)
             .ToListAsync(cancellationToken);
      }
   }
}
