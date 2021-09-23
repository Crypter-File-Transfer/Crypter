using Crypter.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserProfileService : IUserProfileService
   {
      private readonly DataContext _context;

      public UserProfileService(DataContext context)
      {
         _context = context;
      }

      public async Task<IUserProfile> ReadAsync(Guid id)
      {
         return await _context.UserProfile.FindAsync(id);
      }

      public async Task<bool> UpdateAsync(Guid id, string alias, string about)
      {
         var userProfile = await ReadAsync(id);

         if (userProfile == null)
         {
            return false;
         }

         userProfile.Alias = alias;
         userProfile.About = about;

         await _context.SaveChangesAsync();
         return true;
      }
   }
}
