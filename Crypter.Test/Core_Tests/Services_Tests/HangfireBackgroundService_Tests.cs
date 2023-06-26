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

using System;
using System.Threading.Tasks;
using Crypter.Core.Models;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Default;
using Hangfire;
using Moq;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   public class HangfireBackgroundService_Tests
   {
      private TestDataContext _testContext;
      private ICryptoProvider _cryptoProvider;
      private Mock<IBackgroundJobClient> _backgroundJobClientMock;
      private Mock<IEmailService> _emailServiceMock;
      private Mock<ITransferRepository> _transferStorageMock;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _testContext = new TestDataContext(GetType().Name);
         _testContext.EnsureCreated();
      }

      [SetUp]
      public void Setup()
      {
         _cryptoProvider = new DefaultCryptoProvider();
         _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
         _emailServiceMock = new Mock<IEmailService>();
         _transferStorageMock = new Mock<ITransferRepository>();
      }

      [TearDown]
      public void Teardown()
      {
         _testContext.Reset();
      }

      [Test]
      public async Task Verification_Email_Not_Sent_Without_Verification_Parameters()
      {
         _emailServiceMock
            .Setup(x => x.SendEmailVerificationAsync(
               It.IsAny<UserEmailAddressVerificationParameters>()))
            .ReturnsAsync((UserEmailAddressVerificationParameters parameters) => true);

         HangfireBackgroundService sut = new HangfireBackgroundService(_testContext, _backgroundJobClientMock.Object, _cryptoProvider, _emailServiceMock.Object, _transferStorageMock.Object);
         await sut.SendEmailVerificationAsync(Guid.NewGuid());

         _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(It.IsAny<UserEmailAddressVerificationParameters>()), Times.Never);
      }

      [Test]
      public async Task Recovery_Email_Not_Sent_Without_Recovery_Parameters()
      {
         _emailServiceMock
            .Setup(x => x.SendAccountRecoveryLinkAsync(
               It.IsAny<UserRecoveryParameters>(),
               It.IsAny<int>()))
            .ReturnsAsync((UserRecoveryParameters parameters, int expirationMinutes) => true);

         HangfireBackgroundService sut = new HangfireBackgroundService(_testContext, _backgroundJobClientMock.Object, _cryptoProvider, _emailServiceMock.Object, _transferStorageMock.Object);
         await sut.SendRecoveryEmailAsync("foo@test.com");

         _emailServiceMock.Verify(x => x.SendAccountRecoveryLinkAsync(It.IsAny<UserRecoveryParameters>(), It.IsAny<int>()), Times.Never);
      }
   }
}
