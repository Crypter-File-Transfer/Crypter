using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.BouncyCastle
{
   public class AsymmetricWrapper
   {
      /// <summary>
      /// Generate a random asymmetric key pair of the given RSA key size
      /// </summary>
      /// <param name="rsaKeySize"></param>
      /// <returns></returns>
      public AsymmetricCipherKeyPair GenerateAsymmetricKeyPair(RsaKeySize rsaKeySize)
      {
         var random = new SecureRandom();
         var keyGenerationParameters = new KeyGenerationParameters(random, (int)rsaKeySize);
         var generator = new RsaKeyPairGenerator();
         generator.Init(keyGenerationParameters);
         return generator.GenerateKeyPair();
      }

      /// <summary>
      /// Encrypt a message
      /// </summary>
      /// <param name="publicKey"></param>
      /// <returns></returns>
      /// <remarks>
      /// https://stackoverflow.com/questions/10783081/c-sharp-bouncycastle-rsa-encryption-and-decryption
      /// </remarks>
      public byte[] Encrypt(byte[] plaintext, AsymmetricKeyParameter publicKey)
      {
         var engine = new RsaEngine();
         engine.Init(true, publicKey);
         return engine.ProcessBlock(plaintext, 0, plaintext.Length);
      }

      /// <summary>
      /// Decrypt some ciphertext
      /// </summary>
      /// <param name="privateKey"></param>
      /// <returns></returns>
      /// <remarks>
      /// https://stackoverflow.com/questions/10783081/c-sharp-bouncycastle-rsa-encryption-and-decryption
      /// </remarks>
      public byte[] Decrypt(byte[] ciphertext, AsymmetricKeyParameter privateKey)
      {
         var engine = new RsaEngine();
         engine.Init(false, privateKey);
         return engine.ProcessBlock(ciphertext, 0, ciphertext.Length);
      }

      /// <summary>
      /// Generate a signature
      /// </summary>
      /// <param name="plaintext"></param>
      /// <param name="privateKey"></param>
      /// <remarks>https://stackoverflow.com/a/8845111</remarks>
      /// <returns></returns>
      public byte[] DigestAndSign(byte[] plaintext, AsymmetricKeyParameter privateKey)
      {
         var signer = SignerUtilities.GetSigner("SHA256withRSA");
         signer.Init(true, privateKey);

         signer.BlockUpdate(plaintext, 0, plaintext.Length);
         return signer.GenerateSignature();
      }
   }
}
