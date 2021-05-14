
using System;
using System.Collections.Generic;
using System.Linq;
using Crypter.DataAccess.Models;
using Crypter.API.Helpers; 


namespace Crypter.API.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password);
        User GetById(string id);
        User Create(User user, string password);
        void Update(User user, string password = null);
        void UpdatePublic(User user); 
    }

    public class UserService : IUserService
    {
        private DataContext _context;

        public UserService(DataContext context)
        {
            _context = context;
        }

        public User Authenticate(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            var user = _context.Users.SingleOrDefault(x => x.UserName == username);

            // check if username exists
            if (user == null)
                return null;

            // check if password is correct
            if (!VerifyPasswordHash(password, Convert.FromBase64String(user.PasswordHash), Convert.FromBase64String(user.PasswordSalt)))
                return null;

            // authentication successful
            return user;
        }

        public User GetById(string id)
        {
            return _context.Users.Find(id);
        }

        public User Create(User user, string password)
        {
            if (_context.Users.Any(x => x.UserName == user.UserName))
                throw new AppException("Username \"" + user.UserName + "\" is already taken");

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordHash = Convert.ToBase64String(passwordHash);
            user.PasswordSalt = Convert.ToBase64String(passwordSalt); 
            user.UserID = Guid.NewGuid().ToString(); 

            _context.Users.Add(user);
            _context.SaveChanges();

            return user;
        }

        public void UpdatePublic(User userParam)
        {
            var user = _context.Users.Find(userParam.UserID);

            if (user == null)
                throw new AppException("User not found");
            //update public alias
            if (!string.IsNullOrWhiteSpace(userParam.PublicAlias) && userParam.PublicAlias != user.PublicAlias)
            {
                user.PublicAlias = userParam.PublicAlias;
            }
            //update public boolean values
            user.IsPublic = userParam.IsPublic;
            user.AllowAnonMessages = userParam.AllowAnonMessages;
            user.AllowAnonFiles = userParam.AllowAnonFiles;
          
            _context.Users.Update(user);
            _context.SaveChanges();

        }

        public void Update(User userParam, string password = null)
        {
            var user = _context.Users.Find(userParam.UserID);

            if (user == null)
                throw new AppException("User not found");

            // update UserName if it has changed
            if (!string.IsNullOrWhiteSpace(userParam.UserName) && userParam.UserName != user.UserName)
            {
                // throw error if the new username is already taken
                if (_context.Users.Any(x => x.UserName == userParam.UserName))
                    throw new AppException("Username " + userParam.UserName + " is already taken");

                user.UserName = userParam.UserName;
            }

            // update user properties if provided
            if (!string.IsNullOrWhiteSpace(userParam.Email))
                user.Email = userParam.Email;

            // update password if provided
            if (!string.IsNullOrWhiteSpace(password))
            {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(password, out passwordHash, out passwordSalt);

                user.PasswordHash = Convert.ToBase64String(passwordHash);
                user.PasswordSalt = Convert.ToBase64String(passwordSalt); 
            }

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        // private helper methods

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        private static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");
            if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
            if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "passwordHash");

            using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != storedHash[i]) return false;
                }
            }

            return true;
        }

    }
}
