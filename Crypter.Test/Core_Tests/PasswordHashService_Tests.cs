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

namespace Crypter.Test.Core_Tests
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
         (var salt, _) = _sut.MakeSecurePasswordHash(password);
         Assert.True(salt.Length == 16);
      }

      [Test]
      public void Hash_Is_64_Bytes()
      {
         var password = AuthenticationPassword.From("foo");
         (_, var hash) = _sut.MakeSecurePasswordHash(password);
         Assert.True(hash.Length == 64);
      }

      [Test]
      public void Salts_Are_Unique()
      {
         var password = AuthenticationPassword.From("foo");
         (var salt1, _) = _sut.MakeSecurePasswordHash(password);
         (var salt2, _) = _sut.MakeSecurePasswordHash(password);
         Assert.AreNotEqual(salt1, salt2);
      }

      [Test]
      public void Hashes_With_Unique_Salts_Are_Unique()
      {
         var password = AuthenticationPassword.From("foo");
         (_, var hash1) = _sut.MakeSecurePasswordHash(password);
         (_, var hash2) = _sut.MakeSecurePasswordHash(password);
         Assert.AreNotEqual(hash1, hash2);
      }

      [Test]
      public void Hash_Verification_Can_Succeed()
      {
         var password = AuthenticationPassword.From("foo");
         (var salt, var hash) = _sut.MakeSecurePasswordHash(password);
         var hashesMatch = _sut.VerifySecurePasswordHash(password, hash, salt);
         Assert.True(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Fails_With_Bad_Password()
      {
         var password = AuthenticationPassword.From("foo");
         var notPassword = AuthenticationPassword.From("not foo");
         (var salt, var hash) = _sut.MakeSecurePasswordHash(password);
         var hashesMatch = _sut.VerifySecurePasswordHash(notPassword, hash, salt);
         Assert.False(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Fails_With_Bad_Salt()
      {
         var password = AuthenticationPassword.From("foo");
         (var salt, var hash) = _sut.MakeSecurePasswordHash(password);

         // Modify the first byte in the salt to make it "bad"
         salt[0] = salt[0] == 0x01
            ? salt[0] = 0x02
            : salt[0] = 0x01;

         var hashesMatch = _sut.VerifySecurePasswordHash(password, hash, salt);
         Assert.False(hashesMatch);
      }

      [Test]
      public void Hash_Verification_Works_With_Known_Values()
      {
         var password = AuthenticationPassword.From("foo");
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

         var hashesMatch = _sut.VerifySecurePasswordHash(password, hash, salt);
         Assert.True(hashesMatch);
      }
   }
}
