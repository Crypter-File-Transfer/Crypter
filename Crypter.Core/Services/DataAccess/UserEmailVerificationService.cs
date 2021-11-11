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
      private readonly DataContext _context;

      public UserEmailVerificationService(DataContext context)
      {
         _context = context;
      }

      public async Task<bool> InsertAsync(Guid userId, Guid code, byte[] verificationKey)
      {
         var emailVerification = new UserEmailVerification(userId, code, verificationKey, DateTime.UtcNow);
         _context.UserEmailVerification.Add(emailVerification);
         await _context.SaveChangesAsync();
         return true;
      }

      public async Task<IUserEmailVerification> ReadAsync(Guid userId)
      {
         return await _context.UserEmailVerification.FindAsync(userId);
      }

      public async Task<IUserEmailVerification> ReadCodeAsync(Guid code)
      {
         return await _context.UserEmailVerification
            .Where(x => x.Code == code)
            .FirstOrDefaultAsync();
      }

      public async Task DeleteAsync(Guid userId)
      {
         await _context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"UserEmailVerification\" WHERE \"UserEmailVerification\".\"Owner\" = {0}", userId);
      }
   }
}
