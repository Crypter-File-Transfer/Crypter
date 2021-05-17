using Crypter.API.Helpers;
using Crypter.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
    public interface IKeyService
    {
        Task<Key> GetUserPersonalKeyAsync(string userId);
        Task<Key> InsertUserPersonalKeyAsync(string userId, string privateKey, string publicKey);
    }

    public class KeyService : IKeyService
    {
        private readonly DataContext _context;

        public KeyService(DataContext context)
        {
            _context = context;
        }

        public async Task<Key> GetUserPersonalKeyAsync(string userId)
        {
            return await _context.Keys
                .Where(x => x.UserId == userId)
                .Where(x => x.KeyType == KeyType.Personal)
                .FirstOrDefaultAsync();
        }

        public async Task<Key> InsertUserPersonalKeyAsync(string userId, string privateKey, string publicKey)
        {
            if (await GetUserPersonalKeyAsync(userId) != default(Key))
            {
                throw new AppException("User already has a personal key");
            }

            var key = new Key(
                Guid.NewGuid().ToString(),
                userId,
                privateKey,
                publicKey,
                KeyType.Personal,
                DateTime.UtcNow);

            var dbKey = await _context.Keys.AddAsync(key);
            await _context.SaveChangesAsync();
            return dbKey.Entity;
        }
    }
}
