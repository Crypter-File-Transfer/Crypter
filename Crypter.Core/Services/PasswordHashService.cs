using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace Crypter.Core.Services
{
   public class PasswordHashService
   {
      private static readonly int Iterations = 100001;
      private static readonly KeyDerivationPrf KeyDerivationAlgorithm = KeyDerivationPrf.HMACSHA512;
      private static readonly int HashByteLength = 64; // 512 bits
      private static readonly int SaltByteLength = 16; // 128 bits

      public static (byte[] Salt, byte[] Hash) MakeSecurePasswordHash(string password)
      {
         if (string.IsNullOrEmpty(password))
         {
            throw new ArgumentNullException("Password cannot be null or empty", "password");
         }

         byte[] salt = new byte[SaltByteLength];
         using var rng = new RNGCryptoServiceProvider();
         rng.GetNonZeroBytes(salt);

         var hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationAlgorithm, Iterations, HashByteLength);
         return (salt, hash);
      }

      public static bool VerifySecurePasswordHash(string password, byte[] existingHash, byte[] existingSalt)
      {
         if (string.IsNullOrEmpty(password))
         {
            throw new ArgumentNullException("Password cannot be null or empty", "password");
         }

         if (existingHash.Length != HashByteLength)
         {
            throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "existingHash");
         }

         if (existingSalt.Length != SaltByteLength)
         {
            throw new ArgumentException("Invalid length of password salt (16 bytes expected).", "existingHash");
         }

         var computedHash = KeyDerivation.Pbkdf2(password, existingSalt, KeyDerivationAlgorithm, Iterations, HashByteLength);

         for (int i = 0; i < computedHash.Length; i++)
         {
            if (computedHash[i] != existingHash[i])
            {
               return false;
            }
         }

         return true;
      }
   }
}
