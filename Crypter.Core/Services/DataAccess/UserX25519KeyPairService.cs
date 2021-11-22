using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserX25519KeyPairService : IUserPublicKeyPairService<UserX25519KeyPair>
   {
      private readonly DataContext Context;

      public UserX25519KeyPairService(DataContext context)
      {
         Context = context;
      }

      public async Task<IUserPublicKeyPair> GetUserPublicKeyPairAsync(Guid userId)
      {
         return await Context.UserX25519KeyPair
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync();
      }

      public async Task<string> GetUserPublicKeyAsync(Guid userId)
      {
         var keyPair = await Context.UserX25519KeyPair
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync();
         return keyPair?.PublicKey;
      }

      public async Task<bool> InsertUserPublicKeyPairAsync(Guid userId, string privateKey, string publicKey)
      {
         if (await GetUserPublicKeyPairAsync(userId) != default(UserX25519KeyPair))
         {
            return false;
         }

         var key = new UserX25519KeyPair(
             Guid.NewGuid(),
             userId,
             privateKey,
             publicKey,
             DateTime.UtcNow);

         Context.UserX25519KeyPair.Add(key);
         await Context.SaveChangesAsync();
         return true;
      }
   }
}
