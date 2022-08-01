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

using Crypter.ClientServices.Services;
using Crypter.Common.Models;
using Crypter.Common.Primitives;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;

namespace Crypter.Test.ClientServices_Tests
{
   [TestFixture]
   public class PBKDFService_Tests
   {
      private ClientPBKDFService _sut;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         List<PasswordVersion> passwordSettings = new List<PasswordVersion>
         {
            new PasswordVersion
            {
               Version = 1,
               Algorithm = "foo",
               Iterations = 1
            }
         };
         IOptions<List<PasswordVersion>> passwordOptions = Options.Create(passwordSettings);

         _sut = new ClientPBKDFService(passwordOptions);
      }

      [Test]
      public void Deriving_Credential_Key_Is_Predictable()
      {
         byte[] knownKey = new byte[]
         {
            0xba, 0x01, 0xf1, 0xbf, 0x3f, 0x17, 0x95, 0xcc,
            0x51, 0xe6, 0x95, 0xa5, 0x73, 0xc5, 0x75, 0x3d,
            0x04, 0x39, 0x4d, 0x4b, 0xa2, 0xc2, 0x55, 0xea,
            0x6d, 0xe6, 0x2d, 0xc7, 0xe4, 0x10, 0xf5, 0xb8
         };

         Username username = Username.From("jack");
         Password password = Password.From("test");

         byte[] credentialKey = _sut.DeriveUserCredentialKey(username, password, 1);
         Assert.AreEqual(knownKey, credentialKey);
      }

      [Test]
      public void Deriving_Authentication_Password_Is_Predictable()
      {
         string knownAuthenticationPassword = "ugHxvz8XlcxR5pWlc8V1PQQ5TUuiwlXqbeYtx+QQ9biU7HHFLy54AuIwmAKERGcL8Xr7pyyvvuBJAsP5GzE19w==";

         Username username = Username.From("jack");
         Password password = Password.From("test");

         AuthenticationPassword authenticationPassword = _sut.DeriveUserAuthenticationPassword(username, password, 1);
         Assert.AreEqual(knownAuthenticationPassword, authenticationPassword.Value);
      }

      [Test]
      public void Iteration_Count_Effects_Derived_Keys()
      {
         Username username = Username.From("jack");
         Password password = Password.From("test");

         byte[] hashOneRound = _sut.DeriveHashFromCredentials(username, password, 1, ClientPBKDFService.CredentialKeySize);
         byte[] hashTwoRounds = _sut.DeriveHashFromCredentials(username, password, 2, ClientPBKDFService.CredentialKeySize);
         Assert.AreNotEqual(hashOneRound, hashTwoRounds);
      }

      [Test]
      public void Username_Is_Case_Insensitive()
      {
         Username usernameLowercase = Username.From("jack");
         Username usernameUppercase = Username.From("JACK");
         Password password = Password.From("test");

         byte[] hashUppercase = _sut.DeriveHashFromCredentials(usernameLowercase, password, 1, ClientPBKDFService.CredentialKeySize);
         byte[] hashLowercase = _sut.DeriveHashFromCredentials(usernameUppercase, password, 1, ClientPBKDFService.CredentialKeySize);
         Assert.AreEqual(hashUppercase, hashLowercase);
      }

      [Test]
      public void Username_Effects_Derived_Keys()
      {
         Username usernameOne = Username.From("jack");
         Username usernameTwo = Username.From("not-jack");
         Password password = Password.From("test");

         byte[] hashUsernameOne = _sut.DeriveHashFromCredentials(usernameOne, password, 1, ClientPBKDFService.CredentialKeySize);
         byte[] hashUsernameTwo = _sut.DeriveHashFromCredentials(usernameTwo, password, 1, ClientPBKDFService.CredentialKeySize);
         Assert.AreNotEqual(hashUsernameOne, hashUsernameTwo);
      }

      [Test]
      public void Password_Effects_Derived_Keys()
      {
         Username username = Username.From("jack");
         Password passwordOne = Password.From("test");
         Password passwordTwo = Password.From("TEST");

         byte[] hashPasswordOne = _sut.DeriveHashFromCredentials(username, passwordOne, 1, ClientPBKDFService.CredentialKeySize);
         byte[] hashPasswordTwo = _sut.DeriveHashFromCredentials(username, passwordTwo, 1, ClientPBKDFService.CredentialKeySize);
         Assert.AreNotEqual(hashPasswordOne, hashPasswordTwo);
      }
   }
}
