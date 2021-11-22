using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserProfileService : IUserProfileService
   {
      private readonly DataContext Context;

      public UserProfileService(DataContext context)
      {
         Context = context;
      }

      public async Task<IUserProfile> ReadAsync(Guid id)
      {
         return await Context.UserProfile.FindAsync(id);
      }

      public async Task<bool> UpsertAsync(Guid id, string alias, string about)
      {
         var userProfile = await ReadAsync(id);
         if (userProfile == null)
         {
            var newProfile = new UserProfile(id, alias, about, null);
            Context.UserProfile.Add(newProfile);
         }
         else
         {
            userProfile.Alias = alias;
            userProfile.About = about;
         }

         await Context.SaveChangesAsync();
         return true;
      }
   }
}
