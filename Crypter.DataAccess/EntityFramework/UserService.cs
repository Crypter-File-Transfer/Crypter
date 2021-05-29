using Crypter.Contracts.Enum;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Logic;
using Crypter.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.DataAccess.EntityFramework
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

            PasswordLogic.CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            User user = new User(
                Guid.NewGuid(),
                username.ToLower(),
                email?.ToLower(),
                null,
                passwordHash,
                passwordSalt,
                false,
                false,
                false,
                DateTime.UtcNow);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return InsertUserResult.Success;
        }

        public async Task<User> ReadAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<UpdateUserCredentialsResult> UpdateCredentialsAsync(Guid id, string username, string newPassword, string email)
        {
            User user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return UpdateUserCredentialsResult.UserNotFound;
            }

            // TODO - We need to require the user's existing password
            /*
            var passwordsMatch = PasswordLogic.VerifyPasswordHash(existingPassword, user.PasswordHash, user.PasswordSalt);
            if (!passwordsMatch)
            {
                return UpdateUserCredentialsResult.PasswordValidationFailed;
            }
            */

            if (user.UserName != username.ToLower() && !await IsUsernameAvailableAsync(username))
            {
                return UpdateUserCredentialsResult.UsernameUnavailable;
            }

            if (user.Email != email.ToLower() && !await IsEmailAvailableAsync(email))
            {
                return UpdateUserCredentialsResult.EmailUnavailable;
            }

            user.UserName = username.ToLower();
            user.Email = email.ToLower();

            PasswordLogic.CreatePasswordHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return UpdateUserCredentialsResult.Success;
        }

        public async Task<UpdateUserPreferencesResult> UpdatePreferencesAsync(Guid id, string publicAlias, bool isPublic, bool allowAnonymousFiles, bool allowAnonymousMessages)
        {
            User user = await _context.Users
                .FindAsync(id);

            if (user == null)
            {
                return UpdateUserPreferencesResult.UserNotFound;
            }

            user.IsPublic = isPublic;
            user.PublicAlias = publicAlias;
            if (isPublic)
            {
                user.AllowAnonymousFiles = allowAnonymousFiles;
                user.AllowAnonymousMessages = allowAnonymousMessages;
            }
            else
            {
                user.AllowAnonymousFiles = false;
                user.AllowAnonymousMessages = false;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return UpdateUserPreferencesResult.Success;
        }

        public async Task DeleteAsync(Guid id)
        {
            await _context.Database
                .ExecuteSqlRawAsync("DELETE FROM Users WHERE Id = {0}", id);
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == username.ToLower());

            if (user == null)
            {
                return null;
            }

            var passwordsMatch = PasswordLogic.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);
            if (passwordsMatch)
            {
                return user;
            }
            else
            {
                return null;
            }
        }

        public async Task<(int total, IEnumerable<User> users)> SearchByUsernameAsync(string username, int startingIndex, int count)
        {
            var lowerUsername = username.ToLower();

            int totalMatches = await _context.Users
                .Where(x => x.UserName.ToLower().StartsWith(lowerUsername))
                .Where(x => x.IsPublic)
                .CountAsync();

            var users = await _context.Users
                .Where(x => x.UserName.ToLower().StartsWith(lowerUsername))
                .Where(x => x.IsPublic)
                .OrderBy(x => x.UserName)
                .Skip(startingIndex)
                .Take(count)
                .ToListAsync();

            return (totalMatches, users);
        }

        public async Task<(int total, IEnumerable<User> users)> SearchByPublicAliasAsync(string publicAlias, int startingIndex, int count)
        {
            var lowerPublicAlias = publicAlias.ToLower();

            int totalMatches = await _context.Users
                .Where(x => x.PublicAlias.ToLower().StartsWith(lowerPublicAlias))
                .Where(x => x.IsPublic)
                .CountAsync();

            var users = await _context.Users
                .Where(x => x.PublicAlias.ToLower().StartsWith(lowerPublicAlias))
                .Where(x => x.IsPublic)
                .OrderBy(x => x.PublicAlias)
                .Skip(startingIndex)
                .Take(count)
                .ToListAsync();

            return (totalMatches, users);
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            string lowerUsername = username.ToLower();
            return !await _context.Users.AnyAsync(x => x.UserName.ToLower() == lowerUsername);
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            string lowerEmail = email.ToLower();
            return !await _context.Users.AnyAsync(x => x.Email.ToLower() == lowerEmail);
        }

        public async Task<bool> IsRegisteredUserPublicAsync(string username)
        {
            try
            {
                var user = await _context.Users.DefaultIfEmpty().SingleOrDefaultAsync(x => x.UserName == username);
                return user.IsPublic;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<User> ReadPublicUserProfileInformation(string username)
        {
            return await _context.Users.SingleAsync(x => x.UserName == username);
        }

        public async Task<Guid> UserIdFromUsernameAsync(string username)
        {
            var user = await _context.Users.SingleAsync(x => x.UserName == username);
            return user.Id;
        }

        public async Task<bool> MessagesAllowedByUserAsync(Guid recipientId)
        {
            var user = await _context.Users.DefaultIfEmpty().SingleOrDefaultAsync(x => x.Id == recipientId);
            return user.AllowAnonymousMessages;
        }

        public async Task<bool> FilesAllowedByUserAsync(Guid recipientId)
        {
            var user = await _context.Users.DefaultIfEmpty().SingleOrDefaultAsync(x => x.Id == recipientId);
            return user.AllowAnonymousFiles;
        }
    }
}
