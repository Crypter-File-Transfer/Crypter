using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class FileTransferItemService : IBaseTransferService<FileTransfer>
   {
      private readonly DataContext Context;

      public FileTransferItemService(DataContext context)
      {
         Context = context;
      }

      public async Task InsertAsync(FileTransfer item)
      {
         Context.FileTransfer.Add(item);
         await Context.SaveChangesAsync();
      }

      public async Task<FileTransfer> ReadAsync(Guid id)
      {
         return await Context.FileTransfer
             .FindAsync(id);
      }

      public async Task DeleteAsync(Guid id)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"FileTransfer\" WHERE \"FileTransfer\".\"Id\" = {0}", id);
      }

      public async Task<IEnumerable<FileTransfer>> FindBySenderAsync(Guid senderId)
      {
         return await Context.FileTransfer
             .Where(x => x.Sender == senderId)
             .ToListAsync();
      }

      public async Task<IEnumerable<FileTransfer>> FindByRecipientAsync(Guid recipientId)
      {
         return await Context.FileTransfer
             .Where(x => x.Recipient == recipientId)
             .ToListAsync();
      }

      public async Task<IEnumerable<FileTransfer>> FindExpiredAsync()
      {
         return await Context.FileTransfer
             .Where(x => x.Expiration < DateTime.UtcNow)
             .ToListAsync();
      }

      public async Task<long> GetAggregateSizeAsync()
      {
         return await Context.FileTransfer
             .SumAsync(x => x.Size);
      }
   }
}
