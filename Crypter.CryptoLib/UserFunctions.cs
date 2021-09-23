using Crypter.CryptoLib.Enums;
using System;
using System.Text;

namespace Crypter.CryptoLib
{
   public static class UserFunctions
   {
      public static byte[] DigestUserCredentials(string username, string password)
      {
         var digestor = new Crypto.SHA(SHAFunction.SHA512);
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(password));
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(username.ToLower()));
         return digestor.GetDigest();
      }

      /// <summary>
      /// Create the symmetric parameters to encrypt/decrypt a user's stored data
      /// </summary>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <param name="userId"></param>
      /// <returns></returns>
      public static (byte[] Key, byte[] IV) DeriveSymmetricCryptoParamsFromUserDetails(string username, string password, Guid userId)
      {
         var keyDigestor = new Crypto.SHA(SHAFunction.SHA256);
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(username.ToLower()));
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(password));
         var key = keyDigestor.GetDigest();

         var ivDigestor = new Crypto.SHA(SHAFunction.SHA256);
         ivDigestor.BlockUpdate(Encoding.UTF8.GetBytes(userId.ToString().ToLower()));
         var iv = ivDigestor.GetDigest()[0..16];
         return (key, iv);
      }
   }
}
