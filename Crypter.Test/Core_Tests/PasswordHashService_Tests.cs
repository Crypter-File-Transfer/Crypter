using Crypter.Core.Services;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests
{
   public class PasswordHashService_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Salt_Is_16_Bytes()
      {
         (var salt, _) = PasswordHashService.MakeSecurePasswordHash("foo");
         Assert.True(salt.Length == 16);
      }

      [Test]
      public void Hash_Is_64_Bytes()
      {
         (_, var hash) = PasswordHashService.MakeSecurePasswordHash("foo");
         Assert.True(hash.Length == 64);
      }

      [Test]
      public void Salts_Are_Unique()
      {
         var password = "foo";
         (var salt1, _) = PasswordHashService.MakeSecurePasswordHash(password);
         (var salt2, _) = PasswordHashService.MakeSecurePasswordHash(password);
         Assert.AreNotEqual(salt1, salt2);
      }

      [Test]
      public void Hashes_With_Unique_Salts_Are_Unique()
      {
         var password = "foo";
         (_, var hash1) = PasswordHashService.MakeSecurePasswordHash(password);
         (_, var hash2) = PasswordHashService.MakeSecurePasswordHash(password);
         Assert.AreNotEqual(hash1, hash2);
      }

      [Test]
      public void Hash_Verification_Can_Succeed()
      {
         var password = "foo";
         (var salt, var hash) = PasswordHashService.MakeSecurePasswordHash(password);
         var hashesMatch = PasswordHashService.VerifySecurePasswordHash(password, hash, salt);
         Assert.True(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Fails_With_Bad_Password()
      {
         (var salt, var hash) = PasswordHashService.MakeSecurePasswordHash("foo");
         var hashesMatch = PasswordHashService.VerifySecurePasswordHash("not foo", hash, salt);
         Assert.False(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Fails_With_Bad_Salt()
      {
         var password = "foo";
         (var salt, var hash) = PasswordHashService.MakeSecurePasswordHash(password);

         // Modify the first byte in the salt to make it "bad"
         salt[0] = salt[0] == 0x01
            ? salt[0] = 0x02
            : salt[0] = 0x01;

         var hashesMatch = PasswordHashService.VerifySecurePasswordHash(password, hash, salt);
         Assert.False(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Works_With_Known_Values()
      {
         var password = "foo";
         var salt = new byte[]
         {
            0xa1, 0xb8, 0x4d, 0x9b, 0x83, 0xf6, 0xb3, 0x46,
            0xa1, 0x85, 0x2a, 0xc6, 0xee, 0x28, 0x77, 0xe8
         };

         var hash = new byte[]
         {
            0xf8, 0xcd, 0x14, 0x54, 0x7c, 0x79, 0xae, 0x29,
            0x45, 0xc4, 0xe4, 0xb6, 0xf7, 0xf9, 0x0f, 0x00,
            0x5f, 0xd3, 0xac, 0x7b, 0x04, 0x01, 0x51, 0x53,
            0x94, 0x41, 0xd3, 0xf3, 0x42, 0x7e, 0x86, 0xd6,
            0x04, 0x46, 0x77, 0x3a, 0x8b, 0x72, 0x27, 0xe4,
            0xb9, 0x16, 0xb0, 0xc9, 0xbf, 0x6c, 0x49, 0xdd,
            0xd1, 0x30, 0xed, 0x54, 0x5e, 0x2c, 0x22, 0x22,
            0xa1, 0xc7, 0xd8, 0x9d, 0x65, 0xd7, 0x2a, 0x95
         };

         var hashesMatch = PasswordHashService.VerifySecurePasswordHash(password, hash, salt);
         Assert.True(hashesMatch);
      }
   }
}
