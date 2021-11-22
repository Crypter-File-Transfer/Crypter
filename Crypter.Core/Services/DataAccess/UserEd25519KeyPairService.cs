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
      private readonly DataContext Context;

      public UserEd25519KeyPairService(DataContext context)
      {
         Context = context;
      }

      public async Task<IUserPublicKeyPair> GetUserPublicKeyPairAsync(Guid userId)
      {
         return await Context.UserEd25519KeyPair
             .Where(x => x.Owner == userId)
             .FirstOrDefaultAsync();
      }

      public async Task<string> GetUserPublicKeyAsync(Guid userId)
      {
         var keyPair = await Context.UserEd25519KeyPair
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

         Context.UserEd25519KeyPair.Add(key);
         await Context.SaveChangesAsync();
         return true;
      }
   }
}
