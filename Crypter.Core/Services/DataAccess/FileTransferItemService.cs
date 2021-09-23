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
      private readonly DataContext _context;

      public FileTransferItemService(DataContext context)
      {
         _context = context;
      }

      public async Task InsertAsync(FileTransfer item)
      {
         _context.FileTransfer.Add(item);
         await _context.SaveChangesAsync();
      }

      public async Task<FileTransfer> ReadAsync(Guid id)
      {
         return await _context.FileTransfer
             .FindAsync(id);
      }

      public async Task DeleteAsync(Guid id)
      {
         await _context.Database
             .ExecuteSqlRawAsync("DELETE FROM FileTransfer WHERE Id = {0}", id);
      }

      public async Task<IEnumerable<FileTransfer>> FindBySenderAsync(Guid senderId)
      {
         return await _context.FileTransfer
             .Where(x => x.Sender == senderId)
             .ToListAsync();
      }

      public async Task<IEnumerable<FileTransfer>> FindByRecipientAsync(Guid recipientId)
      {
         return await _context.FileTransfer
             .Where(x => x.Recipient == recipientId)
             .ToListAsync();
      }

      public async Task<IEnumerable<FileTransfer>> FindExpiredAsync()
      {
         return await _context.FileTransfer
             .Where(x => x.Expiration < DateTime.UtcNow)
             .ToListAsync();
      }

      public async Task<long> GetAggregateSizeAsync()
      {
         return await _context.FileTransfer
             .SumAsync(x => x.Size);
      }
   }
}
