using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.BouncyCastle
{
   public static class AsymmetricMethods
   {
      /// <summary>
      /// Generate a random asymmetric key pair of the given RSA key size
      /// </summary>
      /// <param name="rsaKeySize"></param>
      /// <returns></returns>
      public static AsymmetricCipherKeyPair GenerateKeys(RsaKeySize rsaKeySize)
      {
         var random = new SecureRandom();
         var keyGenerationParameters = new KeyGenerationParameters(random, (int)rsaKeySize);
         var generator = new RsaKeyPairGenerator();
         generator.Init(keyGenerationParameters);
         return generator.GenerateKeyPair();
      }

      /// <summary>
      /// Encrypt some bytes using RSA
      /// </summary>
      /// <param name="publicKey"></param>
      /// <returns></returns>
      /// <remarks>
      /// https://stackoverflow.com/questions/10783081/c-sharp-bouncycastle-rsa-encryption-and-decryption
      /// </remarks>
      public static byte[] Encrypt(byte[] plaintext, AsymmetricKeyParameter publicKey)
      {
         var engine = new RsaEngine();
         engine.Init(true, publicKey);
         return engine.ProcessBlock(plaintext, 0, plaintext.Length);
      }

      /// <summary>
      /// Decrypt some bytes using RSA
      /// </summary>
      /// <param name="privateKey"></param>
      /// <returns></returns>
      /// <remarks>
      /// https://stackoverflow.com/questions/10783081/c-sharp-bouncycastle-rsa-encryption-and-decryption
      /// </remarks>
      public static byte[] Decrypt(byte[] ciphertext, AsymmetricKeyParameter privateKey)
      {
         var engine = new RsaEngine();
         engine.Init(false, privateKey);
         return engine.ProcessBlock(ciphertext, 0, ciphertext.Length);
      }
   }
}
