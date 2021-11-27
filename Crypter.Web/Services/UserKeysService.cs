using System;
using System.Text;

namespace Crypter.Web.Services
{
   public interface IUserKeysService
   {
      /// <summary>
      /// Generate a new X25519 key pair.
      /// </summary>
      /// <param name="userId"></param>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <returns>Encrypted private key and plaintext public key in PEM format</returns>
      (byte[] encryptedPrivateKey, string publicKey) GenerateNewX25519KeyPair(Guid userId, string username, string password);

      /// <summary>
      /// Generate a new Ed25519 key pair.
      /// </summary>
      /// <param name="userId"></param>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <returns>Encrypted private key and plaintext public key in PEM format</returns>
      public (byte[] encryptedPrivateKey, string publicKey) GenerateNewEd25519KeyPair(Guid userId, string username, string password);

      /// <summary>
      /// Decrypts and returns the provided Curve25519 private key
      /// </summary>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <param name="userId"></param>
      /// <returns>Private key in PEM format</returns>
      public string DecryptPrivateKey(string username, string password, Guid userId, byte[] privateKey);
   }

   public class UserKeysService : IUserKeysService
   {
      public (byte[] encryptedPrivateKey, string publicKey) GenerateNewX25519KeyPair(Guid userId, string username, string password)
      {
         var keyPair = CryptoLib.Crypto.ECDH.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);
         var encryptedPrivateKey = EncryptPrivateKey(privateKey, userId, username, password);

         return (encryptedPrivateKey, publicKey);
      }

      public (byte[] encryptedPrivateKey, string publicKey) GenerateNewEd25519KeyPair(Guid userId, string username, string password)
      {
         var keyPair = CryptoLib.Crypto.ECDSA.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);
         var encryptedPrivateKey = EncryptPrivateKey(privateKey, userId, username, password);

         return (encryptedPrivateKey, publicKey);
      }

      public string DecryptPrivateKey(string username, string password, Guid userId, byte[] privateKey)
      {
         (var key, var iv) = CryptoLib.UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username, password, userId);
         var decrypter = new CryptoLib.Crypto.AES();
         decrypter.Initialize(key, iv, false);
         var decrypted = decrypter.ProcessFinal(privateKey);
         return Encoding.UTF8.GetString(decrypted);
      }

      private static byte[] EncryptPrivateKey(string privatePemKey, Guid userId, string username, string password)
      {
         (var key, var iv) = CryptoLib.UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username.ToLower(), password, userId);
         var encrypter = new CryptoLib.Crypto.AES();
         encrypter.Initialize(key, iv, true);
         return encrypter.ProcessFinal(Encoding.UTF8.GetBytes(privatePemKey));
      }
   }
}
