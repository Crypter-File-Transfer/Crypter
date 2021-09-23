using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserEd25519KeyPairService : IUserPublicKeyPairService<UserEd25519KeyPair>
   {
      private readonly DataContext _context;

      public UserEd25519KeyPairService(DataContext context)
      {
         _context = context;
      }

      public async Task<IUserPublicKeyPair> GetUserPublicKeyPairAsync(Guid userId)
      {
         return await _context.UserEd25519KeyPair
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync();
      }

      public async Task<string> GetUserPublicKeyAsync(Guid userId)
      {
         var keyPair = await _context.UserEd25519KeyPair
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync();
         return keyPair?.PublicKey;
      }

      public async Task<bool> InsertUserPublicKeyPairAsync(Guid userId, string privateKey, string publicKey)
      {
         if (await GetUserPublicKeyPairAsync(userId) != default(UserEd25519KeyPair))
         {
            return false;
         }

         var key = new UserEd25519KeyPair(
             Guid.NewGuid(),
             userId,
             privateKey,
             publicKey,
             DateTime.UtcNow);

         _context.UserEd25519KeyPair.Add(key);
         await _context.SaveChangesAsync();
         return true;
      }
   }
}
