using Crypter.API.Controllers.Methods;
using NUnit.Framework;
using System;

namespace Crypter.Test.API_Tests
{
   [TestFixture]
   public class EmailVerificationEncoder_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void VerificationCode_Encoding_Is_Consistent()
      {
         var verificationCode = new Guid("adec7c67-dfb6-4b9f-9a1f-04c0b0f4643c");
         var knownEncoding = "Z3zsrbbfn0uaHwTAsPRkPA";

         var newEncoding = EmailVerificationEncoder.EncodeVerificationCodeUrlSafe(verificationCode);

         Assert.AreEqual(knownEncoding, newEncoding);
      }

      [Test]
      public void VerificationCode_Decoding_Is_Consistent()
      {
         var encoding = "Z3zsrbbfn0uaHwTAsPRkPA";
         var knownVerificationCode = new Guid("adec7c67-dfb6-4b9f-9a1f-04c0b0f4643c");

         var newCode = EmailVerificationEncoder.DecodeVerificationCodeFromUrlSafe(encoding);

         Assert.AreEqual(knownVerificationCode, newCode);
      }

      [Test]
      public void Signature_Encoding_Is_Consistent()
      {
         byte[] bytes = new byte[] {
            0x8b, 0x6f, 0xa0, 0x13, 0x13, 0xce, 0x51, 0xaf,
            0xc0, 0x9e, 0x61, 0x0f, 0x81, 0x92, 0x50, 0xda,
            0x50, 0x17, 0x78, 0xad, 0x36, 0x3c, 0xba, 0x4f,
            0x9e, 0x31, 0x2a, 0x6e, 0xc8, 0x23, 0xd4, 0x2a
         };
         var knownEncoding = "i2-gExPOUa_AnmEPgZJQ2lAXeK02PLpPnjEqbsgj1Co";

         var newEncoding = EmailVerificationEncoder.EncodeSignatureUrlSafe(bytes);

         Assert.AreEqual(knownEncoding, newEncoding);
      }

      [Test]
      public void Signature_Decoding_Is_Consistent()
      {
         var encoding = "i2-gExPOUa_AnmEPgZJQ2lAXeK02PLpPnjEqbsgj1Co";
         byte[] knownBytes = new byte[] {
            0x8b, 0x6f, 0xa0, 0x13, 0x13, 0xce, 0x51, 0xaf,
            0xc0, 0x9e, 0x61, 0x0f, 0x81, 0x92, 0x50, 0xda,
            0x50, 0x17, 0x78, 0xad, 0x36, 0x3c, 0xba, 0x4f,
            0x9e, 0x31, 0x2a, 0x6e, 0xc8, 0x23, 0xd4, 0x2a
         };

         var newSignature = EmailVerificationEncoder.DecodeSignatureFromUrlSafe(encoding);

         Assert.AreEqual(knownBytes, newSignature);
      }
   }
}
