using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.DataAccess.EntityFramework
{
    public class MessageItemService : IBaseItemService<MessageItem>
    {
        private readonly DataContext _context;

        public MessageItemService(DataContext context)
        {
            _context = context;
        }

        public async Task InsertAsync(MessageItem item)
        {
            _context.Messages.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task<MessageItem> ReadAsync(Guid id)
        {
            return await _context.Messages
                .FindAsync(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _context.Database
                .ExecuteSqlRawAsync("DELETE FROM Messages WHERE Id = {0}", id);
        }

        public async Task<IEnumerable<MessageItem>> FindBySenderAsync(Guid ownerId)
        {
            return await _context.Messages
                .Where(x => x.Sender == ownerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<MessageItem>> FindExpiredAsync()
        {
            return await _context.Messages
                .Where(x => x.Expiration < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<long> GetAggregateSizeAsync()
        {
            return await _context.Messages
                .SumAsync(x => x.Size);
        }
    }
}
