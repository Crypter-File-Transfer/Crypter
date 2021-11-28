using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Enums;
using NUnit.Framework;

namespace Crypter.Test.CryptoLib_Tests
{
   [TestFixture]
   public class SHA_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Sha256_Digest_Works()
      {
         byte[] foo = new byte[] { 0x01 };
         var digestor = new SHA(SHAFunction.SHA256);
         Assert.DoesNotThrow(() => {
            digestor.BlockUpdate(foo);
            digestor.GetDigest();
         });
      }

      [Test]
      public void Sha512_Digest_Works()
      {
         byte[] foo = new byte[] { 0x01 };
         var digestor = new SHA(SHAFunction.SHA512);
         Assert.DoesNotThrow(() => {
            digestor.BlockUpdate(foo);
            digestor.GetDigest();
         });
      }

      [Test]
      public void Sha256_Digests_Are_Unique()
      {
         byte[] first = new byte[]
         {
            0x01, 0x02, 0x03, 0x04
         };

         byte[] second = new byte[]
         {
            0x02, 0x03, 0x04, 0x05
         };

         var firstDigestor = new SHA(SHAFunction.SHA256);
         firstDigestor.BlockUpdate(first);
         var firstDigest = firstDigestor.GetDigest();

         var secondDigestor = new SHA(SHAFunction.SHA256);
         secondDigestor.BlockUpdate(second);
         var secondDigest = secondDigestor.GetDigest();

         Assert.AreNotEqual(firstDigest, secondDigest);
      }

      [Test]
      public void Sha256_Digest_Is_Predictable()
      {
         byte[] knownInput = new byte[] {
            0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20
         };

         byte[] knownHash = new byte[] {
            0x8b, 0x6f, 0xa0, 0x13, 0x13, 0xce, 0x51, 0xaf,
            0xc0, 0x9e, 0x61, 0x0f, 0x81, 0x92, 0x50, 0xda,
            0x50, 0x17, 0x78, 0xad, 0x36, 0x3c, 0xba, 0x4f,
            0x9e, 0x31, 0x2a, 0x6e, 0xc8, 0x23, 0xd4, 0x2a
         };

         var digestor = new SHA(SHAFunction.SHA256);
         digestor.BlockUpdate(knownInput);
         var hashResult = digestor.GetDigest();
         Assert.AreEqual(knownHash, hashResult);
      }

      [Test]
      public void Sha512_Digest_Is_Predictable()
      {
         byte[] knownInput = new byte[] {
            0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20
         };

         byte[] knownHash = new byte[] {
            0xe3, 0x9e, 0x5f, 0xa9, 0x17, 0x94, 0xc9, 0xd7,
            0x52, 0x73, 0x13, 0x43, 0x3d, 0xcc, 0xf7, 0xc4,
            0xec, 0xc0, 0xea, 0xc5, 0x62, 0x67, 0x4e, 0xb6,
            0xee, 0x9b, 0x6e, 0x30, 0x43, 0x4c, 0x92, 0xd3,
            0x35, 0x0b, 0xf6, 0xd9, 0xff, 0x7a, 0x29, 0x9c,
            0x14, 0x34, 0x23, 0xed, 0x74, 0x93, 0x19, 0x75,
            0xdd, 0x16, 0xdf, 0x76, 0x1d, 0x8f, 0x0c, 0xb7,
            0x5b, 0x58, 0x7c, 0x48, 0x4b, 0x54, 0x63, 0xab
         };

         var digestor = new SHA(SHAFunction.SHA512);
         digestor.BlockUpdate(knownInput);
         var hashResult = digestor.GetDigest();
         Assert.AreEqual(knownHash, hashResult);
      }

      [Test]
      public void Sha256_Chunking_Produces_Same_Result()
      {
         byte[] knownInput = new byte[] {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08
         };

         var controlDigestor = new SHA(SHAFunction.SHA256);
         controlDigestor.BlockUpdate(knownInput);
         var controlResult = controlDigestor.GetDigest();

         var chunkedDigestor = new SHA(SHAFunction.SHA256);
         chunkedDigestor.BlockUpdate(knownInput[0..4]);
         chunkedDigestor.BlockUpdate(knownInput[4..8]);
         var chunkedResult = chunkedDigestor.GetDigest();

         Assert.AreEqual(controlResult, chunkedResult);
      }
   }
}
