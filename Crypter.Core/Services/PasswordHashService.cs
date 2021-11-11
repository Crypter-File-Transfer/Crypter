using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace Crypter.Core.Services
{
   /// <summary>
   /// Contains methods to securely hash passwords prior to saving them in the database.
   /// </summary>
   /// <remarks>
   /// Do not move this class outside of Crypter.Core!
   /// Although you may think this class belongs in a more common library,
   ///   nothing outside of Crypter.Core will ever need to perform these functions.
   /// Moving this to a different library runs the risk that it will be distributed with
   ///   front-end applications.  Someone could potentially decompile that library and
   ///   discover our password hashing implementation.
   /// </remarks>
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
            throw new ArgumentNullException("Password cannot be null or empty");
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
            throw new ArgumentNullException("Password cannot be null or empty");
         }

         if (existingHash.Length != HashByteLength)
         {
            throw new ArgumentException("Invalid length of password hash (64 bytes expected).");
         }

         if (existingSalt.Length != SaltByteLength)
         {
            throw new ArgumentException("Invalid length of password salt (16 bytes expected).");
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
