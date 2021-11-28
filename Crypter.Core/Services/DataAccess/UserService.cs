using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserService : IUserService
   {
      private readonly DataContext Context;

      public UserService(DataContext context)
      {
         Context = context;
      }

      public async Task<Guid> InsertAsync(string username, string password, string email)
      {
         (var passwordKey, var passwordHash) = PasswordHashService.MakeSecurePasswordHash(password);

         var user = new User(
             Guid.NewGuid(),
             username,
             email,
             passwordHash,
             passwordKey,
             false,
             DateTime.UtcNow,
             DateTime.MinValue);
         Context.User.Add(user);

         await Context.SaveChangesAsync();
         return user.Id;
      }

      public async Task<IUser> ReadAsync(Guid id)
      {
         return await Context.User.FindAsync(id);
      }

      public async Task<IUser> ReadAsync(string username)
      {
         return await Context.User
            .Where(user => user.Username.ToLower() == username.ToLower())
            .FirstOrDefaultAsync();
      }

      public async Task<UpdateContactInfoResult> UpdateContactInfoAsync(Guid id, string email, string currentPassword)
      {
         var user = await ReadAsync(id);
         var passwordsMatch = PasswordHashService.VerifySecurePasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt);
         if (!passwordsMatch)
         {
            return UpdateContactInfoResult.PasswordValidationFailed;
         }

         user.Email = email;
         user.EmailVerified = false;
         await Context.SaveChangesAsync();
         return UpdateContactInfoResult.Success;
      }

      public async Task UpdateEmailAddressVerification(Guid id, bool isVerified)
      {
         var user = await ReadAsync(id);
         user.EmailVerified = isVerified;
         await Context.SaveChangesAsync();
      }

      public async Task DeleteAsync(Guid id)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"Users\" WHERE \"Users\".\"Id\" = {0}", id);
      }

      public async Task<User> AuthenticateAsync(string username, string password)
      {
         if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
         {
            return null;
         }

         var user = await Context.User.SingleOrDefaultAsync(x => x.Username.ToLower() == username);

         if (user == null)
         {
            return null;
         }

         var passwordsMatch = PasswordHashService.VerifySecurePasswordHash(password, user.PasswordHash, user.PasswordSalt);
         return passwordsMatch
            ? user
            : null;
      }

      public async Task UpdateLastLoginTime(Guid id, DateTime dateTime)
      {
         var user = await ReadAsync(id);
         if (user != null)
         {
            user.LastLogin = dateTime;
            await Context.SaveChangesAsync();
         }
      }

      public async Task<bool> IsUsernameAvailableAsync(string username)
      {
         string lowerUsername = username.ToLower();
         return !await Context.User.AnyAsync(x => x.Username.ToLower() == lowerUsername);
      }

      public async Task<bool> IsEmailAddressAvailableAsync(string email)
      {
         string lowerEmail = email.ToLower();
         return !await Context.User.AnyAsync(x => x.Email.ToLower() == lowerEmail);
      }
   }
}
