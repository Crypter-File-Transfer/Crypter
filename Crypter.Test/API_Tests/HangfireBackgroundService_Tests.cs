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
using MediatR;
using Moq;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Test.API_Tests
{
   [TestFixture]
   public class HangfireBackgroundService_Tests
   {
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
         var mockMediator = new Mock<IMediator>();
         var mockEmailService = new Mock<IEmailService>();

         var sut = new Mock<HangfireBackgroundService>(mockMessageTransferService.Object, mockFileTransferService.Object,
            mockUserService.Object, mockNotificationService.Object, mockUserEmailVerificationService.Object,
            mockEmailService.Object, mockMediator.Object)
         {
            CallBase = true
         };

         await sut.Object.SendEmailVerificationAsync(new Guid(), CancellationToken.None);

         mockEmailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>(), It.IsAny<CancellationToken>()), Times.Never);
         mockEmailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailIsEmpty_EmailNotSent()
      {
         var username = Username.From("jack");
         var noEmailAddress = Maybe<EmailAddress>.None;
         var user = new UserEntity(Guid.NewGuid(), username, noEmailAddress, default, default, false, default, default);

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
         var mockMediator = new Mock<IMediator>();
         var mockEmailService = new Mock<IEmailService>();

         var sut = new Mock<HangfireBackgroundService>(mockMessageTransferService.Object, mockFileTransferService.Object,
            mockUserService.Object, mockNotificationService.Object, mockUserEmailVerificationService.Object,
            mockEmailService.Object, mockMediator.Object)
         {
            CallBase = true
         };

         await sut.Object.SendEmailVerificationAsync(user.Id, CancellationToken.None);

         mockEmailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>(), It.IsAny<CancellationToken>()), Times.Never);
         mockEmailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailAlreadyVerified_EmailNotSent()
      {
         var username = Username.From("jack");
         var emailAddress = EmailAddress.From("jack@crypter.dev");
         var user = new UserEntity(Guid.NewGuid(), username, emailAddress, default, default, true, default, default);

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
         var mockMediator = new Mock<IMediator>();
         var mockEmailService = new Mock<IEmailService>();

         var sut = new Mock<HangfireBackgroundService>(mockMessageTransferService.Object, mockFileTransferService.Object,
            mockUserService.Object, mockNotificationService.Object, mockUserEmailVerificationService.Object,
            mockEmailService.Object, mockMediator.Object)
         {
            CallBase = true
         };

         await sut.Object.SendEmailVerificationAsync(user.Id, CancellationToken.None);

         mockEmailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>(), It.IsAny<CancellationToken>()), Times.Never);
         mockEmailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailVerificationAlreadyPending_EmailNotSent()
      {
         var username = Username.From("jack");
         var emailAddress = EmailAddress.From("jack@crypter.dev");
         var user = new UserEntity(Guid.NewGuid(), username, emailAddress, default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => user);

         var userVerification = new UserEmailVerificationEntity(user.Id, Guid.NewGuid(), default, DateTime.UtcNow);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid userId, CancellationToken cancellationToken) => userVerification);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<IMessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<IFileTransfer>>();
         var mockMediator = new Mock<IMediator>();
         var mockEmailService = new Mock<IEmailService>();

         var sut = new Mock<HangfireBackgroundService>(mockMessageTransferService.Object, mockFileTransferService.Object,
            mockUserService.Object, mockNotificationService.Object, mockUserEmailVerificationService.Object,
            mockEmailService.Object, mockMediator.Object)
         {
            CallBase = true
         };

         await sut.Object.SendEmailVerificationAsync(user.Id, CancellationToken.None);

         mockEmailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<EmailAddress>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>(), It.IsAny<CancellationToken>()), Times.Never);
         mockEmailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_NewUserNeedsEmailVerification_EmailWillSend()
      {
         var username = Username.From("jack");
         var emailAddress = EmailAddress.From("jack@crypter.dev");
         var user = new UserEntity(Guid.NewGuid(), username, emailAddress, default, default, false, default, default);

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
         var mockMediator = new Mock<IMediator>();
         var mockEmailService = new Mock<IEmailService>();

         var sut = new Mock<HangfireBackgroundService>(mockMessageTransferService.Object, mockFileTransferService.Object,
            mockUserService.Object, mockNotificationService.Object, mockUserEmailVerificationService.Object,
            mockEmailService.Object, mockMediator.Object)
         {
            CallBase = true
         };

         await sut.Object.SendEmailVerificationAsync(user.Id, CancellationToken.None);

         mockEmailService.Verify(x => x.SendEmailVerificationAsync(EmailAddress.From(user.Email), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>(), It.IsAny<CancellationToken>()), Times.Once);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>(), default), Times.Never);
      }
   }
}
