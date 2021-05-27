using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.DataAccess.EntityFramework
{
    public class KeyService : IKeyService
    {
        private readonly DataContext _context;

        public KeyService(DataContext context)
        {
            _context = context;
        }

        public async Task<Key> GetUserPersonalKeyAsync(Guid userId)
        {
            return await _context.Keys
                .Where(x => x.Owner == userId)
                .Where(x => x.KeyType == KeyType.Personal)
                .FirstOrDefaultAsync();
        }

        public async Task<string> GetUserPublicKeyAsync(Guid userId)
        {
            var userKeys =  await _context.Keys
                .Where(x => x.Owner == userId)
                .FirstOrDefaultAsync();
            return userKeys?.PublicKey;
        }

        public async Task<bool> InsertUserPersonalKeyAsync(Guid userId, string privateKey, string publicKey)
        {
            if (await GetUserPersonalKeyAsync(userId) != default(Key))
            {
                return false;
            }

            var key = new Key(
                Guid.NewGuid(),
                userId,
                privateKey,
                publicKey,
                KeyType.Personal,
                DateTime.UtcNow);

            _context.Keys.Add(key);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
