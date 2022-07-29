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
using Crypter.Core.Services;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   public class PasswordHashService_Tests
   {
      private PasswordHashService _sut;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _sut = new PasswordHashService();
      }

      [Test]
      public void Service_Exists_In_Crypter_Core()
      {
         var assembly = typeof(PasswordHashService);
         Assert.AreEqual("Crypter.Core.Services.PasswordHashService", assembly.FullName);
      }

      [Test]
      public void Salt_Is_16_Bytes()
      {
         var password = AuthenticationPassword.From("foo");
         var hashOutput = _sut.MakeSecurePasswordHash(password, 1);
         Assert.True(hashOutput.Salt.Length == 16);
      }

      [Test]
      public void Hash_Is_64_Bytes()
      {
         var password = AuthenticationPassword.From("foo");
         var hashOutput = _sut.MakeSecurePasswordHash(password, 1);
         Assert.True(hashOutput.Hash.Length == 64);
      }

      [Test]
      public void Salts_Are_Unique()
      {
         var password = AuthenticationPassword.From("foo");
         var hashOutput1 = _sut.MakeSecurePasswordHash(password, 1);
         var hashOutput2 = _sut.MakeSecurePasswordHash(password, 1);
         Assert.AreNotEqual(hashOutput1.Salt, hashOutput2.Salt);
      }

      [Test]
      public void Hashes_With_Unique_Salts_Are_Unique()
      {
         var password = AuthenticationPassword.From("foo");
         var hashOutput1 = _sut.MakeSecurePasswordHash(password, 1);
         var hashOutput2 = _sut.MakeSecurePasswordHash(password, 1);
         Assert.AreNotEqual(hashOutput1.Hash, hashOutput2.Hash);
      }

      [Test]
      public void Hash_Verification_Can_Succeed()
      {
         var password = AuthenticationPassword.From("foo");
         var hashOutput = _sut.MakeSecurePasswordHash(password, 1);
         var hashesMatch = _sut.VerifySecurePasswordHash(password, hashOutput.Hash, hashOutput.Salt, 1);
         Assert.True(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Fails_With_Bad_Password()
      {
         var password = AuthenticationPassword.From("foo");
         var notPassword = AuthenticationPassword.From("not foo");
         var hashOutput = _sut.MakeSecurePasswordHash(password, 1);
         var hashesMatch = _sut.VerifySecurePasswordHash(notPassword, hashOutput.Hash, hashOutput.Salt, 1);
         Assert.False(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Fails_With_Bad_Salt()
      {
         var password = AuthenticationPassword.From("foo");
         var hashOutput = _sut.MakeSecurePasswordHash(password, 1);

         // Modify the first byte in the salt to make it "bad"
         hashOutput.Salt[0] = hashOutput.Salt[0] == 0x01
            ? hashOutput.Salt[0] = 0x02
            : hashOutput.Salt[0] = 0x01;

         var hashesMatch = _sut.VerifySecurePasswordHash(password, hashOutput.Hash, hashOutput.Salt, 1);
         Assert.False(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Fails_With_Different_Iterations()
      {
         var password = AuthenticationPassword.From("foo");
         var hashOutput = _sut.MakeSecurePasswordHash(password, 1);

         var hashesMatch = _sut.VerifySecurePasswordHash(password, hashOutput.Hash, hashOutput.Salt, 2);
         Assert.False(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Is_Stable()
      {
         var password = AuthenticationPassword.From("foo");
         var salt = new byte[]
         {
            0xa1, 0xb8, 0x4d, 0x9b, 0x83, 0xf6, 0xb3, 0x46,
            0xa1, 0x85, 0x2a, 0xc6, 0xee, 0x28, 0x77, 0xe8
         };

         var hash = new byte[]
         {
            0xb5, 0x61, 0xfa, 0x29, 0xfe, 0x35, 0x27, 0xe1,
            0x54, 0x40, 0x07, 0xc0, 0xc1, 0x8a, 0xf5, 0x4b,
            0x2c, 0x37, 0xfd, 0x91, 0x5b, 0x87, 0xd4, 0x53,
            0xd0, 0xf6, 0x27, 0x8d, 0xa6, 0xe0, 0x62, 0xe5,
            0x17, 0x50, 0x4c, 0xa5, 0x7f, 0x4e, 0x81, 0x07,
            0x76, 0x74, 0x0f, 0x2a, 0x21, 0xb9, 0x17, 0xdb,
            0x56, 0x41, 0xb8, 0x3d, 0x2f, 0xcf, 0x36, 0x01,
            0x21, 0xba, 0x27, 0x65, 0x72, 0x49, 0x80, 0xaf
         };

         var hashesMatch = _sut.VerifySecurePasswordHash(password, hash, salt, 1);
         Assert.True(hashesMatch);
      }
   }
}
