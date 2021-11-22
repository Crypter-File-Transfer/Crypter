using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserEmailVerificationService : IUserEmailVerificationService
   {
      private readonly DataContext Context;

      public UserEmailVerificationService(DataContext context)
      {
         Context = context;
      }

      public async Task<bool> InsertAsync(Guid userId, Guid code, byte[] verificationKey)
      {
         var emailVerification = new UserEmailVerification(userId, code, verificationKey, DateTime.UtcNow);
         Context.UserEmailVerification.Add(emailVerification);
         await Context.SaveChangesAsync();
         return true;
      }

      public async Task<IUserEmailVerification> ReadAsync(Guid userId)
      {
         return await Context.UserEmailVerification.FindAsync(userId);
      }

      public async Task<IUserEmailVerification> ReadCodeAsync(Guid code)
      {
         return await Context.UserEmailVerification
            .Where(x => x.Code == code)
            .FirstOrDefaultAsync();
      }

      public async Task DeleteAsync(Guid userId)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"UserEmailVerification\" WHERE \"UserEmailVerification\".\"Owner\" = {0}", userId);
      }
   }
}
