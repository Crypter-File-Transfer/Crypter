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

      [TestCase(null)]
      [TestCase("")]
      [TestCase(" ")]
      [TestCase("     ")]
      public void Invalid_Passwords_Are_Invalid(string password)
      {
         var result = ValidationService.IsValidPassword(password);
         Assert.IsFalse(result);
      }

      [TestCase("a")]
      [TestCase(" whitespace ")]
      [TestCase("12345")]
      [TestCase("!@#$%^&")]
      public void Valid_Passwords_Are_Valid(string password)
      {
         var result = ValidationService.IsValidPassword(password);
         Assert.IsTrue(result);
      }

      [TestCase(null)]
      [TestCase("")]
      public void Blanks_Are_Not_Considered_Possible_Email_Addresses(string email)
      {
         var result = ValidationService.IsPossibleEmailAddress(email);
         Assert.IsFalse(result);
      }

      [TestCase(" ")]
      [TestCase(" hello ")]
      [TestCase("hello@crypter.dev")]
      public void NonBlanks_Are_Considered_Possible_Email_Addresses(string email)
      {
         var result = ValidationService.IsPossibleEmailAddress(email);
         Assert.IsTrue(result);
      }

      [TestCase(null)]
      [TestCase("")]
      [TestCase(" ")]
      [TestCase("hello@crypter.dev.")]
      public void Invalid_Email_Addresses_Are_Invalid(string email)
      {
         var result = ValidationService.IsValidEmailAddress(email);
         Assert.IsFalse(result);
      }

      [TestCase("jack@crypter.dev")]
      [TestCase("no-reply@crypter.dev")]
      [TestCase("anyone@gmail.com")]
      public void Valid_Email_Addresses_Are_Valid(string email)
      {
         var result = ValidationService.IsValidEmailAddress(email);
         Assert.IsTrue(result);
      }

      [TestCase(null)]
      [TestCase("")]
      [TestCase(" ")]
      [TestCase("Inv@lid")]
      [TestCase("ThisIsExactly33CharactersInLength")]
      public void Invalid_Usernames_Are_Invalid(string username)
      {
         var result = ValidationService.IsValidUsername(username);
         Assert.IsFalse(result);
      }

      [TestCase("jack")]
      [TestCase("JACK")]
      [TestCase("1234567890")]
      [TestCase("_-_-_-_")]
      [TestCase("ThisIsExactly32CharactersInLengt")]
      public void Valid_Usernames_Are_Valid(string username)
      {
         var result = ValidationService.IsValidUsername(username);
         Assert.IsTrue(result);
      }

      [TestCase(" ")]
      [TestCase("`")]
      [TestCase("~")]
      [TestCase("!")]
      [TestCase("@")]
      [TestCase("#")]
      [TestCase("$")]
      [TestCase("%")]
      [TestCase("^")]
      [TestCase("&")]
      [TestCase("*")]
      [TestCase("(")]
      [TestCase(")")]
      [TestCase("=")]
      [TestCase("+")]
      [TestCase("\\")]
      [TestCase("|")]
      [TestCase("[")]
      [TestCase("]")]
      [TestCase("{")]
      [TestCase("}")]
      [TestCase(";")]
      [TestCase(":")]
      [TestCase("'")]
      [TestCase("\"")]
      [TestCase(",")]
      [TestCase("<")]
      [TestCase(".")]
      [TestCase(">")]
      [TestCase("/")]
      [TestCase("?")]
      public void Usernames_May_Not_Contain_Invalid_Characters(string username)
      {
         var result = ValidationService.UsernameMeetsCharacterRequirements(username);
         Assert.IsFalse(result);
      }

      [TestCase("ThisIsExactly33CharactersInLength")]
      public void Usernames_May_Not_Exceed_32_Characters(string username)
      {
         var result = ValidationService.UsernameMeetsLengthRequirements(username);
         Assert.IsFalse(result);
      }

      [TestCase(null)]
      [TestCase("")]
      public void Usernames_May_Not_Be_Empty(string username)
      {
         var result = ValidationService.UsernameMeetsLengthRequirements(username);
         Assert.IsFalse(result);
      }
   }
}
