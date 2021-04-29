using Crypter.CryptoLib.Enums;
using NUnit.Framework;

namespace Crypter.Test.CryptoTests
{
   public class SymmetricTests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Symmetric_KeyGen_Produces_256_Bit_Keys()
      {
         var key = CryptoLib.BouncyCastle.SymmetricMethods.GenerateKey(AesKeySize.AES256);
         Assert.AreEqual(32, key.GetKey().Length);
      }

      [Test]
      public void Symmetric_KeyGen_Produces_192_Bit_Keys()
      {
         var key = CryptoLib.BouncyCastle.SymmetricMethods.GenerateKey(AesKeySize.AES192);
         Assert.AreEqual(24, key.GetKey().Length);
      }

      [Test]
      public void Symmetric_KeyGen_Produces_128_Bit_Keys()
      {
         var key = CryptoLib.BouncyCastle.SymmetricMethods.GenerateKey(AesKeySize.AES128);
         Assert.AreEqual(16, key.GetKey().Length);
      }

      [Test]
      public void Symmetric_KeyGen_Produces_Unique_Keys()
      {
         var key1 = CryptoLib.BouncyCastle.SymmetricMethods.GenerateKey(AesKeySize.AES256);
         var key2 = CryptoLib.BouncyCastle.SymmetricMethods.GenerateKey(AesKeySize.AES256);
         Assert.AreNotEqual(key1.GetKey(), key2.GetKey());
      }

      [Test]
      public void IV_Generation_Produces_128_Bit_Values()
      {
         var iv = CryptoLib.BouncyCastle.SymmetricMethods.GenerateIV();
         Assert.AreEqual(16, iv.Length);
      }

      [Test]
      public void IV_Generation_Produces_Unique_Values()
      {
         var iv1 = CryptoLib.BouncyCastle.SymmetricMethods.GenerateIV();
         var iv2 = CryptoLib.BouncyCastle.SymmetricMethods.GenerateIV();
         Assert.AreNotEqual(iv1, iv2);
      }

      [Test]
      public void Symmetric_Encryption_Is_Predictable()
      {
         var knownKey = new byte[] {
            0x41, 0x73, 0xc0, 0xd2, 0xe7, 0x1a, 0xe5, 0x4f,
            0xe1, 0x90, 0x83, 0x8f, 0x2e, 0x5a, 0xc7, 0xfc
         };

         var knownIV = new byte[]
         {
            0x5e, 0xdd, 0xed, 0x1a, 0x92, 0xa4, 0x89, 0x31,
            0x81, 0xb6, 0xa3, 0x47, 0xf6, 0xed, 0x8a, 0x6a
         };

         var knownPlaintext = new byte[]
         {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f
         };

         var knownCiphertext = new byte[]
         {
            0x38, 0x1c, 0xda, 0x9d, 0x68, 0xe7, 0xbf, 0x4b,
            0x96, 0x13, 0x3f, 0xc8, 0x35, 0xbb, 0x52, 0x35
         };

         var symmetricParams = CryptoLib.Common.MakeSymmetricCryptoParams(knownKey, knownIV);
         var newCiphertext = CryptoLib.BouncyCastle.SymmetricMethods.Encrypt(knownPlaintext, symmetricParams.Key, symmetricParams.IV);
         Assert.AreEqual(knownCiphertext, newCiphertext);
      }

      [Test]
      public void Symmetric_Decryption_Is_Predictable()
      {
         var knownKey = new byte[] {
            0x41, 0x73, 0xc0, 0xd2, 0xe7, 0x1a, 0xe5, 0x4f,
            0xe1, 0x90, 0x83, 0x8f, 0x2e, 0x5a, 0xc7, 0xfc
         };

         var knownIV = new byte[]
         {
            0x5e, 0xdd, 0xed, 0x1a, 0x92, 0xa4, 0x89, 0x31,
            0x81, 0xb6, 0xa3, 0x47, 0xf6, 0xed, 0x8a, 0x6a
         };

         var knownPlaintext = new byte[]
         {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f
         };

         var knownCiphertext = new byte[]
         {
            0x38, 0x1c, 0xda, 0x9d, 0x68, 0xe7, 0xbf, 0x4b,
            0x96, 0x13, 0x3f, 0xc8, 0x35, 0xbb, 0x52, 0x35
         };

         var symmetricParams = CryptoLib.Common.MakeSymmetricCryptoParams(knownKey, knownIV);
         var newPlaintext = CryptoLib.BouncyCastle.SymmetricMethods.Decrypt(knownCiphertext, symmetricParams.Key, symmetricParams.IV);
         Assert.AreEqual(knownPlaintext, newPlaintext);
      }
   }
}
