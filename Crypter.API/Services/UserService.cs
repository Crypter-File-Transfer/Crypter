using System;
using System.Linq;
using Crypter.DataAccess.Models;
using Crypter.API.Helpers;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
    public interface IUserService
    {
        User Authenticate(string username, string password);
        User GetById(string id);
        Task<IEnumerable<User>> SearchByUsername(string username);
        Task<IEnumerable<User>> SearchByPublicAlias(string publicAlias);
        public List<UploadItem> GetUploadsById(string id);
        User Create(User user, string password);
        void Update(User user, string password = null);
        void UpdatePublic(User user);
        void Delete(string UserID); 
    }

    public class UserService : IUserService
    {
        private readonly DataContext _context;

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

        public async Task<IEnumerable<User>> SearchByUsername(string username)
        {
            var lowerUsername = username.ToLower();
            return await _context.Users
                .Where(x => x.UserName.ToLower().StartsWith(lowerUsername))
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> SearchByPublicAlias(string publicAlias)
        {
            var lowerPublicAlias = publicAlias.ToLower();
            return await _context.Users
                .Where(x => x.PublicAlias.ToLower().StartsWith(lowerPublicAlias))
                .ToListAsync();
        }

        public List<UploadItem> GetUploadsById(string userid)
        {
            var userMessageUploads = (from upload in _context.MessageUploads where upload.UserID == userid select new { upload.UntrustedName, upload.Size, upload.ExpirationDate});
            var userFileUploads = (from upload in _context.FileUploads where upload.UserID == userid select new { upload.UntrustedName, upload.Size, upload.ExpirationDate });

            var userUploads = (from upload in userMessageUploads.Concat(userFileUploads) orderby upload.ExpirationDate select upload);

            if (userUploads == null)
            {
                return null;
            }

            List<UploadItem> uploads = new List<UploadItem>();
            foreach (var item in userUploads)
            {
                var uploaditem = new UploadItem(
                        item.UntrustedName,
                        item.Size,
                        item.ExpirationDate
                    );
                Console.WriteLine($"Filename: {item.UntrustedName}\nSize: {item.Size}\nExpiration: {item.ExpirationDate} ");
                uploads.Add(uploaditem);
            }
            //return list of all uploads
            return uploads;
        }

        public User Create(User user, string password)
        {
            if (_context.Users.Any(x => x.UserName == user.UserName))
                throw new AppException("Username \"" + user.UserName + "\" is already taken");

            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

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
            if (!userParam.IsPublic)
            {
                user.IsPublic = false;
                user.AllowAnonFiles = false;
                user.AllowAnonMessages = false;
            }
            else
            {
                user.IsPublic = userParam.IsPublic;
                user.AllowAnonMessages = userParam.AllowAnonMessages;
                user.AllowAnonFiles = userParam.AllowAnonFiles;
            }
          
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
                CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

                user.PasswordHash = Convert.ToBase64String(passwordHash);
                user.PasswordSalt = Convert.ToBase64String(passwordSalt); 
            }

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        //delete user
        public void Delete(string UserID)
        {
            var user = _context.Users.Find(UserID); 
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges(); 
            }
        }

        // private helper methods

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using var hmac = new System.Security.Cryptography.HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
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
