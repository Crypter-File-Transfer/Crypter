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
      private readonly DataContext _context;

      public UserService(DataContext context)
      {
         _context = context;
      }

      public async Task<InsertUserResult> InsertAsync(string username, string password, string email)
      {
         if (string.IsNullOrEmpty(username))
         {
            return InsertUserResult.EmptyUsername;
         }

         if (string.IsNullOrEmpty(password))
         {
            return InsertUserResult.EmptyPassword;
         }

         if (email == "")
         {
            return InsertUserResult.EmptyEmail;
         }

         if (!await IsUsernameAvailableAsync(username))
         {
            return InsertUserResult.UsernameTaken;
         }

         (var passwordKey, var passwordHash) = PasswordHashService.MakeSecurePasswordHash(password);

         var user = new User(
             Guid.NewGuid(),
             username.ToLower(),
             email?.ToLower(),
             passwordHash,
             passwordKey,
             false,
             DateTime.UtcNow,
             DateTime.MinValue);
         _context.User.Add(user);

         var userProfile = new UserProfile(user.Id, null, null, null);
         _context.UserProfile.Add(userProfile);

         var userPrivacy = new UserPrivacy(user.Id, false, UserVisibilityLevel.None, UserItemTransferPermission.None, UserItemTransferPermission.None);
         _context.UserPrivacy.Add(userPrivacy);

         await _context.SaveChangesAsync();
         return InsertUserResult.Success;
      }

      public async Task<IUser> ReadAsync(Guid id)
      {
         return await _context.User.FindAsync(id);
      }

      public async Task<IUser> ReadAsync(string username)
      {
         return await _context.User
            .Where(user => user.Username.ToLower() == username.ToLower())
            .FirstOrDefaultAsync();
      }

      public async Task<UpdateContactInfoResult> UpdateContactInfoAsync(Guid id, string email, string currentPassword)
      {
         var user = await ReadAsync(id);

         if (user == null)
         {
            return UpdateContactInfoResult.UserNotFound;
         }

         var passwordsMatch = PasswordHashService.VerifySecurePasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt);
         if (!passwordsMatch)
         {
            return UpdateContactInfoResult.PasswordValidationFailed;
         }

         if (user.Email != email.ToLower() && !await IsEmailAddressAvailableAsync(email))
         {
            return UpdateContactInfoResult.EmailUnavailable;
         }

         user.Email = email.ToLower();
         user.EmailVerified = false;
         await _context.SaveChangesAsync();
         return UpdateContactInfoResult.Success;
      }

      public async Task DeleteAsync(Guid id)
      {
         await _context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"Users\" WHERE \"Users\".\"Id\" = {0}", id);
      }

      public async Task<User> AuthenticateAsync(string username, string password)
      {
         if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
         {
            return null;
         }

         var user = await _context.User.SingleOrDefaultAsync(x => x.Username.ToLower() == username);

         if (user == null)
         {
            return null;
         }

         var passwordsMatch = PasswordHashService.VerifySecurePasswordHash(password, user.PasswordHash, user.PasswordSalt);
         return passwordsMatch
            ? user
            : null;
      }

      public async Task<bool> IsUsernameAvailableAsync(string username)
      {
         string lowerUsername = username.ToLower();
         return !await _context.User.AnyAsync(x => x.Username.ToLower() == lowerUsername);
      }

      public async Task<bool> IsEmailAddressAvailableAsync(string email)
      {
         string lowerEmail = email.ToLower();
         return !await _context.User.AnyAsync(x => x.Email.ToLower() == lowerEmail);
      }
   }
}
