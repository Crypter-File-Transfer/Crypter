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

using Crypter.API.Models;
using Crypter.API.Services;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Core.Entities;
using Crypter.Core.Interfaces;
using Moq;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Test.API_Tests
{
   [TestFixture]
   public class EmailService_Tests
   {
      private EmailSettings _emailSettings;

      [OneTimeSetUp]
      public void OneTimeSetup()
      {
         _emailSettings = new EmailSettings();
      }

      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public async Task HangfireEmailVerification_UserDoesNotExist_EmailNotSent()
      {
         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => null);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<IMessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<IFileTransfer>>();

         var emailService = new Mock<EmailService>(_emailSettings, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(new Guid());

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailIsEmpty_EmailNotSent()
      {
         var username = Username.From("jack");
         var noEmailAddress = Maybe<EmailAddress>.None;
         var user = new User(Guid.NewGuid(), username, noEmailAddress, default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => user);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<IMessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<IFileTransfer>>();

         var emailService = new Mock<EmailService>(_emailSettings, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailAlreadyVerified_EmailNotSent()
      {
         var username = Username.From("jack");
         var emailAddress = EmailAddress.From("jack@crypter.dev");
         var user = new User(Guid.NewGuid(), username, emailAddress, default, default, true, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => user);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<IMessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<IFileTransfer>>();

         var emailService = new Mock<EmailService>(_emailSettings, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailVerificationAlreadyPending_EmailNotSent()
      {
         var username = Username.From("jack");
         var emailAddress = EmailAddress.From("jack@crypter.dev");
         var user = new User(Guid.NewGuid(), username, emailAddress, default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => user);

         var userVerification = new UserEmailVerification(user.Id, Guid.NewGuid(), default, DateTime.UtcNow);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => userVerification);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<IMessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<IFileTransfer>>();

         var emailService = new Mock<EmailService>(_emailSettings, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_NewUserNeedsEmailVerification_EmailWillSend()
      {
         var username = Username.From("jack");
         var emailAddress = EmailAddress.From("jack@crypter.dev");
         var user = new User(Guid.NewGuid(), username, emailAddress, default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => user);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<IMessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<IFileTransfer>>();

         var emailService = new Mock<EmailService>(_emailSettings, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(EmailAddress.From(user.Email), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Once);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), EmailAddress.From(user.Email)), Times.Once);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task ServiceDisabled_SendAsync_ReturnsFalse()
      {
         var mockUserService = new Mock<IUserService>();
         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<IMessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<IFileTransfer>>();

         var emailService = new Mock<EmailService>(_emailSettings, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         var emailAddress = EmailAddress.From("jack@crypter.dev");
         var result = await emailService.Object.SendAsync("foo", "bar", emailAddress);
         Assert.False(result);
      }
   }
}
