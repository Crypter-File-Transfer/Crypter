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
      private readonly DataContext _context;

      public MessageTransferItemService(DataContext context)
      {
         _context = context;
      }

      public async Task InsertAsync(MessageTransfer item)
      {
         _context.MessageTransfer.Add(item);
         await _context.SaveChangesAsync();
      }

      public async Task<MessageTransfer> ReadAsync(Guid id)
      {
         return await _context.MessageTransfer
             .FindAsync(id);
      }

      public async Task DeleteAsync(Guid id)
      {
         await _context.Database
             .ExecuteSqlRawAsync("DELETE FROM MessageTransfer WHERE Id = {0}", id);
      }

      public async Task<IEnumerable<MessageTransfer>> FindBySenderAsync(Guid ownerId)
      {
         return await _context.MessageTransfer
             .Where(x => x.Sender == ownerId)
             .ToListAsync();
      }

      public async Task<IEnumerable<MessageTransfer>> FindByRecipientAsync(Guid recipientId)
      {
         return await _context.MessageTransfer
             .Where(x => x.Recipient == recipientId)
             .ToListAsync();
      }

      public async Task<IEnumerable<MessageTransfer>> FindExpiredAsync()
      {
         return await _context.MessageTransfer
             .Where(x => x.Expiration < DateTime.UtcNow)
             .ToListAsync();
      }

      public async Task<long> GetAggregateSizeAsync()
      {
         return await _context.MessageTransfer
             .SumAsync(x => x.Size);
      }
   }
}
