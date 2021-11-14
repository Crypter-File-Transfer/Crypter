using Crypter.API.Services;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Test.API_Tests
{
   [TestFixture]
   public class EmailService_Tests
   {
      IConfiguration ServiceConfiguration { get; set; }

      [OneTimeSetUp]
      public void OneTimeSetup()
      {
         var configurationSettings = new Dictionary<string, string> {
            { "EmailSettings:Enabled", "false" },
            { "EmailSettings:From", "no-reply" },
            { "EmailSettings:Username", "no-reply" },
            { "EmailSettings:Password", "secure" },
            { "EmailSettings:Host", "localhost" },
            { "EmailSettings:Port", "555" }
         };

         ServiceConfiguration = new ConfigurationBuilder()
             .AddInMemoryCollection(configurationSettings)
             .Build();
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
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => null);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<MessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<FileTransfer>>();

         var emailService = new Mock<EmailService>(ServiceConfiguration, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(new Guid());

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>()), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailIsNull_EmailNotSent()
      {
         var user = new User(Guid.NewGuid(), default, null, default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => user);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<MessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<FileTransfer>>();

         var emailService = new Mock<EmailService>(ServiceConfiguration, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>()), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailIsEmpty_EmailNotSent()
      {
         var user = new User(Guid.NewGuid(), default, "", default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => user);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<MessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<FileTransfer>>();

         var emailService = new Mock<EmailService>(ServiceConfiguration, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>()), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailAlreadyVerified_EmailNotSent()
      {
         var user = new User(Guid.NewGuid(), default, "jack@crypter.dev", default, default, true, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => user);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<MessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<FileTransfer>>();

         var emailService = new Mock<EmailService>(ServiceConfiguration, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>()), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_UserEmailVerificationAlreadyPending_EmailNotSent()
      {
         var user = new User(Guid.NewGuid(), default, "jack@crypter.dev", default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => user);

         var userVerification = new UserEmailVerification(user.Id, Guid.NewGuid(), default, DateTime.UtcNow);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => userVerification);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<MessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<FileTransfer>>();

         var emailService = new Mock<EmailService>(ServiceConfiguration, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Never);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>()), Times.Never);
      }

      [Test]
      public async Task HangfireEmailVerification_NewUserNeedsEmailVerification_EmailWillSend()
      {
         var user = new User(Guid.NewGuid(), default, "jack@crypter.dev", default, default, false, default, default);

         var mockUserService = new Mock<IUserService>();
         mockUserService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => user);

         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();
         mockUserEmailVerificationService
            .Setup(x => x.ReadAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid userId) => null);

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<MessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<FileTransfer>>();

         var emailService = new Mock<EmailService>(ServiceConfiguration, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         await emailService.Object.HangfireSendEmailVerificationAsync(user.Id);

         emailService.Verify(x => x.SendEmailVerificationAsync(user.Email, It.IsAny<Guid>(), It.IsAny<AsymmetricKeyParameter>()), Times.Once);
         emailService.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), user.Email), Times.Once);
         mockUserEmailVerificationService.Verify(x => x.InsertAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<byte[]>()), Times.Never);
      }

      [Test]
      public async Task ServiceDisabled_SendAsync_ReturnsFalse()
      {
         var mockUserService = new Mock<IUserService>();
         var mockUserEmailVerificationService = new Mock<IUserEmailVerificationService>();

         var mockNotificationService = new Mock<IUserNotificationSettingService>();
         var mockMessageTransferService = new Mock<IBaseTransferService<MessageTransfer>>();
         var mockFileTransferService = new Mock<IBaseTransferService<FileTransfer>>();

         var emailService = new Mock<EmailService>(ServiceConfiguration, mockUserService.Object, mockUserEmailVerificationService.Object,
            mockNotificationService.Object, mockMessageTransferService.Object, mockFileTransferService.Object)
         {
            CallBase = true
         };
         var result = await emailService.Object.SendAsync("foo", "bar", "jack@crypter.dev");
         Assert.False(result);
      }
   }
}
