using Crypter.CryptoLib.Enums;
using NUnit.Framework;

namespace Crypter.Test.CryptoTests
{
   public class DigestTests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Sha256_Digest_Works()
      {
         byte[] foo = new byte[] { 0x00 };
         Assert.DoesNotThrow(() => CryptoLib.BouncyCastle.DigestMethods.GetDigest(foo, DigestAlgorithm.SHA256));
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
         var hashResult = CryptoLib.BouncyCastle.DigestMethods.GetDigest(knownInput, DigestAlgorithm.SHA256);
         Assert.AreEqual(knownHash, hashResult);
      }
   }
}
