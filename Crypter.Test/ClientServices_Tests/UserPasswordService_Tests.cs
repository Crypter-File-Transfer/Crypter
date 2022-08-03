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
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication;
using NUnit.Framework;
using System;

namespace Crypter.Test.ClientServices_Tests
{
   [TestFixture]
   internal class UserPasswordService_Tests
   {
      private UserPasswordService _sut;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _sut = new UserPasswordService();
      }

      [Test]
      public void Authentication_Password_Derivation_Cannot_Exceed_Current_Password_Version()
      {
         int wrongPasswordVersion = _sut.CurrentPasswordVersion + 1;

         Username username = Username.From("crypter");
         Password password = Password.From("P@ssw0rd?");

         Assert.Throws<NotImplementedException>(() => _sut.DeriveUserAuthenticationPassword(username, password, wrongPasswordVersion));
      }

      [Test]
      public void Credential_Key_Derivation_Cannot_Exceed_Current_Password_Version()
      {
         int wrongPasswordVersion = _sut.CurrentPasswordVersion + 1;

         Username username = Username.From("crypter");
         Password password = Password.From("P@ssw0rd?");

         Assert.Throws<NotImplementedException>(() => _sut.DeriveUserCredentialKey(username, password, wrongPasswordVersion));
      }

      [Test]
      public void Sha512_Authentication_Password_Derivation_Is_Deterministic()
      {
         Username username = Username.From("crypter");
         Password password = Password.From("P@ssw0rd?");

         string knownEncodedPassword = "BZRRT9oSiHi88TPk7cCbT+qrBtQqwUe70L4rMSvC3e17hpiMhzS13gPbeNuI+Mz7oKXPm8cirs+3oc3anELI4g==";

#pragma warning disable CS0618
         VersionedPassword versionedPassword = UserPasswordService.DeriveSha512AuthenticationPassword(username, password);
#pragma warning restore CS0618

         Assert.AreEqual(0, versionedPassword.Version);
         Assert.AreEqual(knownEncodedPassword, versionedPassword.Password);
      }

      [Test]
      public void Argon_Authentication_Password_Derivation_Is_Deterministic()
      {
         Username username = Username.From("crypter");
         Password password = Password.From("P@ssw0rd?");

         string knownEncodedPassword = "NqtTGwKf0wxUSYOqB5Eu0mZLEYMCmWgz6qXl2wRltjji1D4/kpbS+fUqeVFTJP4gvi0fJh36sGmOSjYdrViG7Q==";

         VersionedPassword versionedPassword = UserPasswordService.DeriveArgonAuthenticationPassword(username, password);

         Assert.AreEqual(1, versionedPassword.Version);
         Assert.AreEqual(knownEncodedPassword, versionedPassword.Password);
      }

      [Test]
      public void Sha256_Credential_Key_Derivation_Is_Deterministic()
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

#pragma warning disable CS0618
         byte[] key = UserPasswordService.DeriveSha256CredentialKey(username, password);
#pragma warning restore CS0618
         Assert.AreEqual(knownKey, key);
      }

      [Test]
      public void Argon_Credential_Key_Derivation_Is_Deterministic()
      {
         Username username = Username.From("Samwise");
         Password password = Password.From("Gamgee");

         byte[] knownKey = new byte[]
         {
            0x43, 0x61, 0xef, 0x39, 0x84, 0xc0, 0x70, 0xd3,
            0x9a, 0x18, 0xa5, 0xc1, 0xd8, 0xe6, 0x0f, 0x7f,
            0x9f, 0xbf, 0x5b, 0x37, 0x07, 0x21, 0x76, 0xc4,
            0x36, 0x51, 0x46, 0xe4, 0xdd, 0x2c, 0xaf, 0xee
         };

         byte[] key = UserPasswordService.DeriveArgonKey(username, password, _sut.CredentialKeySize);
         Assert.AreEqual(knownKey, key);
      }

      [Test]
      public void Authentication_Password_Derivation_Uses_Sha512_For_Version_0()
      {
         var username = Username.From("crypter");
         var password = Password.From("P@ssw0rd?");

         string knownEncodedPassword = "BZRRT9oSiHi88TPk7cCbT+qrBtQqwUe70L4rMSvC3e17hpiMhzS13gPbeNuI+Mz7oKXPm8cirs+3oc3anELI4g==";

         VersionedPassword versionedPassword = _sut.DeriveUserAuthenticationPassword(username, password, 0);

         Assert.AreEqual(0, versionedPassword.Version);
         Assert.AreEqual(knownEncodedPassword, versionedPassword.Password);
      }

      [Test]
      public void Authentication_Password_Derivation_Uses_Argon_For_Version_1()
      {
         var username = Username.From("crypter");
         var password = Password.From("P@ssw0rd?");

         string knownEncodedPassword = "NqtTGwKf0wxUSYOqB5Eu0mZLEYMCmWgz6qXl2wRltjji1D4/kpbS+fUqeVFTJP4gvi0fJh36sGmOSjYdrViG7Q==";

         VersionedPassword versionedPassword = _sut.DeriveUserAuthenticationPassword(username, password, 1);

         Assert.AreEqual(1, versionedPassword.Version);
         Assert.AreEqual(knownEncodedPassword, versionedPassword.Password);
      }

      [Test]
      public void Credential_Key_Derivation_Uses_Sha256_For_Version_0()
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

         byte[] key = _sut.DeriveUserCredentialKey(username, password, 0);
         Assert.AreEqual(knownKey, key);
      }

      [Test]
      public void Credential_Key_Derivation_Uses_Argon_For_Version_1()
      {
         Username username = Username.From("Samwise");
         Password password = Password.From("Gamgee");

         byte[] knownKey = new byte[]
         {
            0x43, 0x61, 0xef, 0x39, 0x84, 0xc0, 0x70, 0xd3,
            0x9a, 0x18, 0xa5, 0xc1, 0xd8, 0xe6, 0x0f, 0x7f,
            0x9f, 0xbf, 0x5b, 0x37, 0x07, 0x21, 0x76, 0xc4,
            0x36, 0x51, 0x46, 0xe4, 0xdd, 0x2c, 0xaf, 0xee
         };

         byte[] key = _sut.DeriveUserCredentialKey(username, password, 1);
         Assert.AreEqual(knownKey, key);
      }

      [TestCase(0)]
      [TestCase(1)]
      public void Username_Is_Case_Insensitive_For_Authentication_Password_Derivation(int passwordVersion)
      {
         Username usernameLowercase = Username.From("username");
         Username usernameUppercase = Username.From("USERNAME");
         Password password = Password.From("P@ssw0rd?");

         VersionedPassword versionedPasswordLowercase = _sut.DeriveUserAuthenticationPassword(usernameLowercase, password, passwordVersion);
         VersionedPassword versionedPasswordUppercase = _sut.DeriveUserAuthenticationPassword(usernameUppercase, password, passwordVersion);

         Assert.AreEqual(versionedPasswordLowercase.Password, versionedPasswordUppercase.Password);
      }

      [TestCase(0)]
      [TestCase(1)]
      public void Username_Is_Case_Insensitive_For_Credential_Key_Derivation(int passwordVersion)
      {
         Username usernameLowercase = Username.From("username");
         Username usernameUppercase = Username.From("USERNAME");
         Password password = Password.From("P@ssw0rd?");

         byte[] versionedPasswordLowercase = _sut.DeriveUserCredentialKey(usernameLowercase, password, passwordVersion);
         byte[] versionedPasswordUppercase = _sut.DeriveUserCredentialKey(usernameUppercase, password, passwordVersion);

         Assert.AreEqual(versionedPasswordLowercase, versionedPasswordUppercase);
      }

      [TestCase(0)]
      [TestCase(1)]
      public void Password_Is_Case_Sensitive_For_Authentication_Password_Derivation(int passwordVersion)
      {
         Username username = Username.From("username");
         Password passwordLowercase = Password.From("password");
         Password passwordUppercase = Password.From("PASSWORD");

         VersionedPassword versionedPasswordLowercase = _sut.DeriveUserAuthenticationPassword(username, passwordLowercase, passwordVersion);
         VersionedPassword versionedPasswordUppercase = _sut.DeriveUserAuthenticationPassword(username, passwordUppercase, passwordVersion);

         Assert.AreNotEqual(versionedPasswordLowercase.Password, versionedPasswordUppercase.Password);
      }

      [TestCase(0)]
      [TestCase(1)]
      public void Password_Is_Case_Sensitive_For_Credential_Key_Derivation(int passwordVersion)
      {
         Username username = Username.From("username");
         Password passwordLowercase = Password.From("password");
         Password passwordUppercase = Password.From("PASSWORD");

         byte[] versionedPasswordLowercase = _sut.DeriveUserCredentialKey(username, passwordLowercase, passwordVersion);
         byte[] versionedPasswordUppercase = _sut.DeriveUserCredentialKey(username, passwordUppercase, passwordVersion);

         Assert.AreNotEqual(versionedPasswordLowercase, versionedPasswordUppercase);
      }
   }
}
