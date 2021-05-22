using System;
using System.Security.Cryptography;
using System.Text;

namespace Crypter.DataAccess.Logic
{
    public static class PasswordLogic
    {
        public static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("Password cannot be null or empty", "password");
            }

            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public static bool VerifyPasswordHash(string password, byte[] existingHash, byte[] existingSalt)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("Password cannot be null or empty", "password");
            }

            if (existingHash.Length != 64)
            {
                throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "existingHash");
            }

            if (existingSalt.Length != 128)
            {
                throw new ArgumentException("Invalid length of password salt (128 bytes expected).", "existingHash");
            }

            using (var hmac = new HMACSHA512(existingSalt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                if (computedHash.Length != existingHash.Length)
                {
                    throw new ApplicationException("Length of computed hash and length of existing hash do not match.");
                }
                
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != existingHash[i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
