using Crypter.Common.Services;
using NUnit.Framework;

namespace Crypter.Test.Common_Tests
{
   [TestFixture]
   public class ValidationService_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Null_Is_Invalid_Password()
      {
         string password = null;
         var result = ValidationService.IsValidPassword(password);

         Assert.IsFalse(result);
      }

      [Test]
      public void Empty_String_Is_Invalid_Password()
      {
         var password = "";
         var result = ValidationService.IsValidPassword(password);

         Assert.IsFalse(result);
      }

      [Test]
      public void Whitespace_Is_Invalid_Password()
      {
         var password = " ";
         var result = ValidationService.IsValidPassword(password);

         Assert.IsFalse(result);
      }

      [Test]
      public void Text_Is_Valid_Password()
      {
         var password = "text";
         var result = ValidationService.IsValidPassword(password);

         Assert.IsTrue(result);
      }

      [Test]
      public void Null_Is_Invalid_Email_Address()
      {
         string email = null;
         var result = ValidationService.IsValidEmailAddress(email);

         Assert.IsFalse(result);
      }

      [Test]
      public void Empty_String_Is_Invalid_Email_Address()
      {
         var email = "";
         var result = ValidationService.IsValidEmailAddress(email);

         Assert.IsFalse(result);
      }

      [Test]
      public void Whitespace_Is_Invalid_Email_Address()
      {
         var email = " ";
         var result = ValidationService.IsValidEmailAddress(email);

         Assert.IsFalse(result);
      }

      [Test]
      public void Trailing_Period_Is_Invalid_Email_Address()
      {
         var email = "jack@crypter.dev.";
         var result = ValidationService.IsValidEmailAddress(email);

         Assert.IsFalse(result);
      }

      [Test]
      public void Actual_Email_Address_Is_Valid_Email_Address()
      {
         var email = "jack@crypter.dev";
         var result = ValidationService.IsValidEmailAddress(email);

         Assert.IsTrue(result);
      }
   }
}
