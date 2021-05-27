using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.DataAccess.EntityFramework
{
    public class FileItemService : IBaseItemService<FileItem>
    {
        private readonly DataContext _context;

        public FileItemService(DataContext context)
        {
            _context = context;
        }

        public async Task InsertAsync(FileItem item)
        {
            _context.Files.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task<FileItem> ReadAsync(Guid id)
        {
            return await _context.Files
                .FindAsync(id);
        }

        public async Task DeleteAsync(Guid id)
        {
            await _context.Database
                .ExecuteSqlRawAsync("DELETE FROM Files WHERE Id = {0}", id);
        }

        public async Task<IEnumerable<FileItem>> FindBySenderAsync(Guid senderId)
        {
            return await _context.Files
                .Where(x => x.Sender == senderId)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileItem>> FindByRecipientAsync(Guid recipientId)
        {
            return await _context.Files
                .Where(x => x.Recipient == recipientId)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileItem>> FindExpiredAsync()
        {
            return await _context.Files
                .Where(x => x.Expiration < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<long> GetAggregateSizeAsync()
        {
            return await _context.Files
                .SumAsync(x => x.Size);
        }
    }
}
