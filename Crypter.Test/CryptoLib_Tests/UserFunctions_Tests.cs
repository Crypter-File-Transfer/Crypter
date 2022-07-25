/*
 * Copyright (C) 2022 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.Common.Primitives;
using Crypter.CryptoLib;
using NUnit.Framework;
using System;

namespace Crypter.Test.CryptoLib_Tests
{
   [TestFixture]
   public class UserFunctions_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void User_Credentials_Are_Digested()
      {
         var username = Username.From("crypter");
         var password = Password.From("P@ssw0rd?");

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

         AuthenticationPassword authPassword = UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
         Assert.AreEqual(knownDigest, Convert.FromBase64String(authPassword.Value));
      }

      [Test]
      public void User_Credentials_Are_Digested_Username_Is_Case_Insensitive()
      {
         Username usernameLowercase = Username.From("username");
         Username usernameUppercase = Username.From("USERNAME");
         Password password = Password.From("P@ssw0rd?");

         AuthenticationPassword lowercaseDigest = UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(usernameLowercase, password);
         AuthenticationPassword uppercaseDigest = UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(usernameUppercase, password);
         Assert.AreEqual(lowercaseDigest.Value, uppercaseDigest.Value);
      }

      [Test]
      public void User_Credentials_Are_Digested_Password_Is_Case_Sensitive()
      {
         Username username = Username.From("Frodo");
         Password passwordLowercase = Password.From("password");
         Password passwordUppercase = Password.From("PASSWORD");

         AuthenticationPassword lowercaseDigest = UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, passwordLowercase);
         AuthenticationPassword uppercaseDigest = UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, passwordUppercase);
         Assert.AreNotEqual(lowercaseDigest.Value, uppercaseDigest.Value);
      }

      [Test]
      public void Symmetric_Key_Can_Be_Derived_From_User_Login_Information()
      {
         Username username = Username.From("Samwise");
         Password password = Password.From("Gamgee");

         byte[] knownKey = new byte[]
         {
            0x53, 0xb3, 0x5e, 0x2d, 0xfb, 0xac, 0x4e, 0x88,
            0xea, 0x86, 0x6a, 0x63, 0xb6, 0x52, 0xc2, 0x64,
            0xb3, 0xd7, 0x2e, 0xf4, 0x9f, 0x10, 0xff, 0x15,
            0xfb, 0x91, 0x38, 0x41, 0x2d, 0xa4, 0xde, 0x52
         };

         byte[] key = UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, password);
         Assert.AreEqual(knownKey, key);
      }

      [Test]
      public void Symmetric_Key_Derivation_Outputs_Same_Key_Regardless_Of_Username_Capitalization()
      {
         Username usernameLowercase = Username.From("gimli");
         Username usernameUppercase = Username.From("GIMLI");
         Password password = Password.From("TheDwarf");

         byte[] lowercaseKey = UserFunctions.DeriveSymmetricKeyFromUserCredentials(usernameLowercase, password);
         byte[] uppercaseKey = UserFunctions.DeriveSymmetricKeyFromUserCredentials(usernameUppercase, password);
         Assert.AreEqual(lowercaseKey, uppercaseKey);
      }

      [Test]
      public void Symmetric_Key_Derivation_Different_Key_When_Password_Capitalization_Changes()
      {
         Username username = Username.From("Aragon");
         Password lowercasePassword = Password.From("son_of_arathorn");
         Password uppercasePassword = Password.From("SON_OF_ARATHORN");

         byte[] lowercaseKey = UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, lowercasePassword);
         byte[] uppercaseKey = UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, uppercasePassword);
         Assert.AreNotEqual(lowercaseKey, uppercaseKey);
      }
   }
}
