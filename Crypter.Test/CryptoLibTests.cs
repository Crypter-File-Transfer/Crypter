using Crypter.CryptoLib;
using Crypter.CryptoLib.Enums;
using NUnit.Framework;

namespace Crypter.Test
{
   public class CryptoLibTests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Sha256_Digest_Works()
      {
         byte[] foo = new byte[] { 0x00 };
         Assert.DoesNotThrow(() => CryptoLib.BouncyCastle.DigestWrapper.GetDigest(foo, DigestAlgorithm.SHA256));
      }

      [Test]
      public void Asymmetric_KeyGen_Works()
      {
         var wrapper = new CryptoLib.BouncyCastle.AsymmetricWrapper();
         Assert.DoesNotThrow(() => wrapper.GenerateAsymmetricKeyPair(RsaKeySize._1024));
      }

      [Test]
      public void Symmetric_KeyGen_Produces_256_Bit_Keys()
      {
         var wrapper = new CryptoLib.BouncyCastle.SymmetricWrapper();
         var key = wrapper.GenerateSymmetricKey(AesKeySize.AES256);
         Assert.AreEqual(32, key.GetKey().Length);
      }

      [Test]
      public void Symmetric_KeyGen_Produces_192_Bit_Keys()
      {
         var wrapper = new CryptoLib.BouncyCastle.SymmetricWrapper();
         var key = wrapper.GenerateSymmetricKey(AesKeySize.AES192);
         Assert.AreEqual(24, key.GetKey().Length);
      }

      [Test]
      public void Symmetric_KeyGen_Produces_128_Bit_Keys()
      {
         var wrapper = new CryptoLib.BouncyCastle.SymmetricWrapper();
         var key = wrapper.GenerateSymmetricKey(AesKeySize.AES128);
         Assert.AreEqual(16, key.GetKey().Length);
      }

      [Test]
      public void Symmetric_KeyGen_Produces_Unique_Keys()
      {
         var wrapper = new CryptoLib.BouncyCastle.SymmetricWrapper();
         var key1 = wrapper.GenerateSymmetricKey(AesKeySize.AES256);
         var key2 = wrapper.GenerateSymmetricKey(AesKeySize.AES256);
         Assert.AreNotEqual(key1.GetKey(), key2.GetKey());
      }

      [Test]
      public void IV_Generation_Produces_128_Bit_Values()
      {
         var wrapper = new CryptoLib.BouncyCastle.SymmetricWrapper();
         var iv = wrapper.GenerateIV();
         Assert.AreEqual(16, iv.Length);
      }

      [Test]
      public void IV_Generation_Produces_Unique_Values()
      {
         var wrapper = new CryptoLib.BouncyCastle.SymmetricWrapper();
         var iv1 = wrapper.GenerateIV();
         var iv2 = wrapper.GenerateIV();
         Assert.AreNotEqual(iv1, iv2);
      }

      [Test]
      public void Repeatable_Sha256_Hash()
      {
         byte[] knownInput = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
         byte[] knownHash = new byte[] {
            0x90, 0xa5, 0x12, 0xa5, 0xb8, 0xa1, 0x07, 0x06,
            0x5c, 0x11, 0xe1, 0x03, 0xaa, 0xd9, 0xc4, 0xa9,
            0xd9, 0xbc, 0x5f, 0x8b, 0x92, 0xac, 0x4d, 0x96,
            0x2c, 0x42, 0xdc, 0xc1, 0x01, 0x0f, 0x9d, 0xdb
         };
         var hashResult = CryptoLib.BouncyCastle.DigestWrapper.GetDigest(knownInput, DigestAlgorithm.SHA256);
         Assert.AreEqual(knownHash, hashResult);
      }
   }
}