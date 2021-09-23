using Crypter.CryptoLib;
using NUnit.Framework;
using System;

namespace Crypter.Test.CryptoLib_Tests
{
   public class UserFunctions_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void User_Credentials_Are_Digested()
      {
         var username = "crypter";
         var password = "P@ssw0rd?";

         var knownDigest = new byte[]
         {
            0x05, 0x94, 0x51, 0x4f, 0xda, 0x12, 0x88, 0x78,
            0xbc, 0xf1, 0x33, 0xe4, 0xed, 0xc0, 0x9b, 0x4f,
            0xea, 0xab, 0x06, 0xd4, 0x2a, 0xc1, 0x47, 0xbb,
            0xd0, 0xbe, 0x2b, 0x31, 0x2b, 0xc2, 0xdd, 0xed,
            0x7b, 0x86, 0x98, 0x8c, 0x87, 0x34, 0xb5, 0xde,
            0x03, 0xdb, 0x78, 0xdb, 0x88, 0xf8, 0xcc, 0xfb,
            0xa0, 0xa5, 0xcf, 0x9b, 0xc7, 0x22, 0xae, 0xcf,
            0xb7, 0xa1, 0xcd, 0xda, 0x9c, 0x42, 0xc8, 0xe2
         };

         var digestedCredentials = UserFunctions.DigestUserCredentials(username, password);
         Assert.AreEqual(knownDigest, digestedCredentials);
      }

      [Test]
      public void User_Credentials_Are_Digested_Username_Is_Case_Insensitive()
      {
         var usernameLowercase = "username";
         var usernameUppercase = "USERNAME";
         var password = "P@ssw0rd?";

         var lowercaseDigest = UserFunctions.DigestUserCredentials(usernameLowercase, password);
         var uppercaseDigest = UserFunctions.DigestUserCredentials(usernameUppercase, password);
         Assert.AreEqual(lowercaseDigest, uppercaseDigest);
      }

      [Test]
      public void User_Credentials_Are_Digested_Password_Is_Case_Sensitive()
      {
         var username = "Frodo";
         var passwordLowercase = "password";
         var passwordUppercase = "PASSWORD";

         var lowercaseDigest = UserFunctions.DigestUserCredentials(username, passwordLowercase);
         var uppercaseDigest = UserFunctions.DigestUserCredentials(username, passwordUppercase);
         Assert.AreNotEqual(lowercaseDigest, uppercaseDigest);
      }

      [Test]
      public void Symmetric_Crypto_Params_Can_Be_Derived_From_User_Details()
      {
         var username = "Samwise";
         var password = "Gamgee";
         var userId = Guid.Parse("77164afe-2c54-4b4a-b2aa-1a35bf2101b9");

         var knownKey = new byte[]
         {
            0x53, 0xb3, 0x5e, 0x2d, 0xfb, 0xac, 0x4e, 0x88,
            0xea, 0x86, 0x6a, 0x63, 0xb6, 0x52, 0xc2, 0x64,
            0xb3, 0xd7, 0x2e, 0xf4, 0x9f, 0x10, 0xff, 0x15,
            0xfb, 0x91, 0x38, 0x41, 0x2d, 0xa4, 0xde, 0x52
         };

         var knownIV = new byte[]
         {
            0x18, 0xd0, 0xaf, 0x02, 0x1f, 0x4f, 0xa9, 0x13,
            0xb1, 0xe5, 0x22, 0x1b, 0x37, 0x52, 0xf5, 0x22
         };

         (var key, var iv) = UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username, password, userId);
         Assert.AreEqual(knownKey, key);
         Assert.AreEqual(knownIV, iv);
      }

      [Test]
      public void Symmetric_Crypto_Params_Can_Be_Derived_From_User_Details_Username_Is_Case_Insensitive()
      {
         var usernameLowercase = "gimli";
         var usernameUppercase = "GIMLI";
         var password = "TheDwarf";
         var userId = Guid.NewGuid();

         (var lowercaseKey, var lowercaseIV) = UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(usernameLowercase, password, userId);
         (var uppercaseKey, var uppercaseIV) = UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(usernameUppercase, password, userId);
         Assert.AreEqual(lowercaseKey, uppercaseKey);
         Assert.AreEqual(lowercaseIV, uppercaseIV);
      }

      [Test]
      public void Symmetric_Crypto_Params_Can_Be_Derived_From_User_Details_Password_Is_Case_Sensitive()
      {
         var username = "Aragon";
         var lowercasePassword = "son_of_arathorn";
         var uppercasePassword = "SON_OF_ARATHORN";
         var userId = Guid.NewGuid();

         (var lowercaseKey, var lowercaseIV) = UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username, lowercasePassword, userId);
         (var uppercaseKey, var uppercaseIV) = UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username, uppercasePassword, userId);
         Assert.AreNotEqual(lowercaseKey, uppercaseKey);
         Assert.AreEqual(lowercaseIV, uppercaseIV); // IV is derived from the userId
      }
   }
}
