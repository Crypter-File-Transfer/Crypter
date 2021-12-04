/*
 * Copyright (C) 2021 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commerical license options.
 */

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
      public void Null_Is_Not_A_Possible_Email_Address()
      {
         string email = null;
         var result = ValidationService.IsPossibleEmailAddress(email);

         Assert.IsFalse(result);
      }

      [Test]
      public void Empty_String_Is_Not_A_Possible_Email_Address()
      {
         string email = "";
         var result = ValidationService.IsPossibleEmailAddress(email);

         Assert.IsFalse(result);
      }

      [Test]
      public void Whitespace_Is_A_Possible_Email_Address()
      {
         string email = " ";
         var result = ValidationService.IsPossibleEmailAddress(email);

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

      [Test]
      public void Null_Is_Not_A_Valid_Username()
      {
         string username = null;
         var result = ValidationService.IsValidUsername(username);

         Assert.IsFalse(result);
      }

      [Test]
      public void Empty_String_Is_Not_A_Valid_Username()
      {
         string username = "";
         var result = ValidationService.IsValidUsername(username);

         Assert.IsFalse(result);
      }

      [Test]
      public void Whitespace_Is_A_Valid_Username()
      {
         string username = " ";
         var result = ValidationService.IsValidUsername(username);

         Assert.IsTrue(result);
      }
   }
}
