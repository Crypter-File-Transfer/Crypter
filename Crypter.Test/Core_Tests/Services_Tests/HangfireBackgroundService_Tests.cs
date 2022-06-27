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

using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   public class HangfireBackgroundService_Tests
   {
      private TestDataContext _testContext;
      private IUserService _userService;
      private Mock<IUserEmailVerificationService> _userEmailVerificationMock;
      private Mock<IEmailService> _emailServiceMock;
      private Mock<ITransferStorageService> _transferStorageMock;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _testContext = new TestDataContext(GetType().Name);
         _testContext.EnsureCreated();

         _userService = new UserService(_testContext);
      }

      [SetUp]
      public void Setup()
      {
         _userEmailVerificationMock = new Mock<IUserEmailVerificationService>();
         _emailServiceMock = new Mock<IEmailService>();
         _transferStorageMock = new Mock<ITransferStorageService>();
      }

      [TearDown]
      public void Teardown()
      {
         _testContext.Reset();
      }

      [Test]
      public async Task Verification_Email_Not_Sent_Without_Verification_Parameters()
      {
         _userEmailVerificationMock
            .Setup(x => x.CreateNewVerificationParametersAsync(
               It.IsAny<Guid>(),
               It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => Maybe<UserEmailAddressVerificationParameters>.None);

         _emailServiceMock
            .Setup(x => x.SendEmailVerificationAsync(
               It.IsAny<UserEmailAddressVerificationParameters>(),
               It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEmailAddressVerificationParameters parameters, CancellationToken cancellationToken) => true);

         HangfireBackgroundService sut = new HangfireBackgroundService(_testContext, _userService, _userEmailVerificationMock.Object, _emailServiceMock.Object, _transferStorageMock.Object);
         await sut.SendEmailVerificationAsync(Guid.NewGuid(), CancellationToken.None);

         _userEmailVerificationMock.Verify(x => x.CreateNewVerificationParametersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
         _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(It.IsAny<UserEmailAddressVerificationParameters>(), It.IsAny<CancellationToken>()), Times.Never);
         _userEmailVerificationMock.Verify(x => x.SaveSentVerificationParametersAsync(It.IsAny<UserEmailAddressVerificationParameters>(), It.IsAny<CancellationToken>()), Times.Never);
      }

      [Test]
      public async Task Verification_Email_Is_Sent_When_Given_Verification_Parameters()
      {
         string publicKey = @"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VuAyEAj5qskz931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PUBLIC KEY-----
".ReplaceLineEndings();
         var parameters = new UserEmailAddressVerificationParameters(Guid.NewGuid(), EmailAddress.From("test@test.com"), Guid.NewGuid(), new byte[] { 0x00 }, PEMString.From(publicKey));

         _userEmailVerificationMock
            .Setup(x => x.CreateNewVerificationParametersAsync(
               It.IsAny<Guid>(),
               It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => parameters);

         _emailServiceMock
            .Setup(x => x.SendEmailVerificationAsync(
               It.IsAny<UserEmailAddressVerificationParameters>(),
               It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEmailAddressVerificationParameters parameters, CancellationToken cancellationToken) => true);

         _userEmailVerificationMock
            .Setup(x => x.SaveSentVerificationParametersAsync(
               It.IsAny<UserEmailAddressVerificationParameters>(),
               It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEmailAddressVerificationParameters parameters, CancellationToken cancellationToken) => 1);

         HangfireBackgroundService sut = new HangfireBackgroundService(_testContext, _userService, _userEmailVerificationMock.Object, _emailServiceMock.Object, _transferStorageMock.Object);
         await sut.SendEmailVerificationAsync(parameters.UserId, CancellationToken.None);

         _userEmailVerificationMock.Verify(x => x.CreateNewVerificationParametersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
         _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(It.IsAny<UserEmailAddressVerificationParameters>(), It.IsAny<CancellationToken>()), Times.Once);
         _userEmailVerificationMock.Verify(x => x.SaveSentVerificationParametersAsync(It.IsAny<UserEmailAddressVerificationParameters>(), It.IsAny<CancellationToken>()), Times.Once);
      }
   }
}
