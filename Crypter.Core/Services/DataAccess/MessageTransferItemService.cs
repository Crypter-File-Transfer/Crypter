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
