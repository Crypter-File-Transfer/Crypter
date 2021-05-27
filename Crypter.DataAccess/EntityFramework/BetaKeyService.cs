using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.DataAccess.EntityFramework
{
    public class BetaKeyService : IBetaKeyService
    {
        private readonly DataContext _context;

        public BetaKeyService(DataContext context)
        {
            _context = context;
        }

        public async Task InsertAsync(string key)
        {
            var betaKey = new BetaKey { Key = key };
            _context.BetaKeys.Add(betaKey);
            await _context.SaveChangesAsync();
        }

        public async Task<BetaKey> ReadAsync(string key)
        {
            return await _context.BetaKeys
                .Where(x => x.Key == key)
                .FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(string key)
        {
            await _context.Database
                .ExecuteSqlRawAsync("DELETE FROM BetaKeys WHERE `Key` = {0}", key);
        }
    }
}
