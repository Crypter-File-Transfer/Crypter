/*
 * Copyright (C) 2023 Crypter File Transfer
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
using Crypter.Core.Entities;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Default;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   internal class UserEmailVerificationService_Tests
   {
      private ICryptoProvider _cryptoProvider;
      private UserEmailVerificationService _sut;
      private TestDataContext _testContext;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _testContext = new TestDataContext(GetType().Name);
         _testContext.EnsureCreated();
         _cryptoProvider = new DefaultCryptoProvider();
         _sut = new UserEmailVerificationService(_testContext, _cryptoProvider);
      }

      [TearDown]
      public void Teardown()
      {
         _testContext.Reset();
      }

      [Test]
      public async Task Verification_Parameters_Not_Created_If_User_Does_Not_Exist()
      {
         var result = await _sut.GenerateVerificationParametersAsync(Guid.NewGuid());
         Assert.IsTrue(result.IsNone);
      }

      [Test]
      public async Task Verification_Parameters_Not_Created_If_User_Email_Already_Verified()
      {
         UserEntity newUser = new UserEntity(Guid.NewGuid(), Username.From("test"), EmailAddress.From("jack@test.com"), new byte[] { 0x00 }, new byte[] { 0x00 }, 1, 1, true, DateTime.UtcNow, DateTime.UtcNow);
         _testContext.Users.Add(newUser);
         await _testContext.SaveChangesAsync();

         var result = await _sut.GenerateVerificationParametersAsync(newUser.Id);
         Assert.IsTrue(result.IsNone);
      }

      [Test]
      public async Task Verification_Parameters_Not_Created_If_User_Verification_Already_Pending()
      {
         UserEntity newUser = new UserEntity(Guid.NewGuid(), Username.From("test"), EmailAddress.From("jack@test.com"), new byte[] { 0x00 }, new byte[] { 0x00 }, 1, 1, false, DateTime.UtcNow, DateTime.UtcNow);
         newUser.EmailVerification = new UserEmailVerificationEntity(newUser.Id, Guid.NewGuid(), new byte[] { 0x00 }, DateTime.UtcNow);

         _testContext.Users.Add(newUser);
         await _testContext.SaveChangesAsync();

         var result = await _sut.GenerateVerificationParametersAsync(newUser.Id);
         Assert.IsTrue(result.IsNone);
      }

      [TestCase(null)]
      [TestCase("")]
      [TestCase("invalid email address")]
      public async Task Verification_Parameters_Not_Created_If_User_Email_Address_Is_Invalid(string emailAddress)
      {
         UserEntity newUser = new UserEntity(Guid.NewGuid(), "username", emailAddress, new byte[] { 0x00 }, new byte[] { 0x00 }, 1, 1, false, DateTime.UtcNow, DateTime.UtcNow);
         _testContext.Users.Add(newUser);
         await _testContext.SaveChangesAsync();

         var result = await _sut.GenerateVerificationParametersAsync(newUser.Id);
         Assert.IsTrue(result.IsNone);
      }

      [Test]
      public async Task Verification_Parameters_Created_If_All_Criteria_Are_Satisfied()
      {
         UserEntity newUser = new UserEntity(Guid.NewGuid(), Username.From("test"), EmailAddress.From("jack@test.com"), new byte[] { 0x00 }, new byte[] { 0x00 }, 1, 1, false, DateTime.UtcNow, DateTime.UtcNow);
         _testContext.Users.Add(newUser);
         await _testContext.SaveChangesAsync();

         var result = await _sut.GenerateVerificationParametersAsync(newUser.Id);
         Assert.IsTrue(result.IsSome);
         result.IfSome(x =>
         {
            Assert.AreEqual(newUser.Id, x.UserId);
            Assert.AreEqual(newUser.EmailAddress, x.EmailAddress.Value);

            Span<byte> verificationCode = x.VerificationCode.ToByteArray();
            bool verificationCodeVerified = _cryptoProvider.DigitalSignature.VerifySignature(x.VerificationKey, verificationCode, x.Signature);
            Assert.IsTrue(verificationCodeVerified);
         });
      }
   }
}
