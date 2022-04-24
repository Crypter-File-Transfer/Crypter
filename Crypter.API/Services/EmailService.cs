﻿/*
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

using Crypter.API.Methods;
using Crypter.API.Models;
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Core.Interfaces;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Org.BouncyCastle.Crypto;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public interface IEmailService
   {
      Task<bool> SendAsync(string subject, string message, EmailAddress recipient);
      Task<bool> SendEmailVerificationAsync(EmailAddress emailAddress, Guid verificationCode, AsymmetricKeyParameter ecdsaPrivateKey);
      Task HangfireSendEmailVerificationAsync(Guid userId);
      Task<bool> SendTransferNotificationAsync(EmailAddress emailAddress);
      Task HangfireSendTransferNotificationAsync(TransferItemType itemType, Guid itemId);
   }

   public class EmailService : IEmailService
   {
      private readonly EmailSettings Settings;

      private readonly IUserService UserService;
      private readonly IUserEmailVerificationService UserEmailVerificationService;
      private readonly IUserNotificationSettingService UserNotificationSettingService;
      private readonly IBaseTransferService<IMessageTransferItem> MessageTransferService;
      private readonly IBaseTransferService<IFileTransferItem> FileTransferService;

      public EmailService(EmailSettings emailSettings, IUserService userService, IUserEmailVerificationService userEmailVerificationService, IUserNotificationSettingService userNotificationSettingService,
         IBaseTransferService<IMessageTransferItem> messageTransferService, IBaseTransferService<IFileTransferItem> fileTransferService)
      {
         Settings = emailSettings;
         UserService = userService;
         UserEmailVerificationService = userEmailVerificationService;
         UserNotificationSettingService = userNotificationSettingService;
         MessageTransferService = messageTransferService;
         FileTransferService = fileTransferService;
      }

      /// <summary>
      /// Send an email to the provided recipient.
      /// </summary>
      /// <param name="subject"></param>
      /// <param name="message"></param>
      /// <param name="recipient"></param>
      /// <remarks>This method is 'virtual' to enable some unit tests.</remarks>
      /// <returns></returns>
      public virtual async Task<bool> SendAsync(string subject, string message, EmailAddress recipient)
      {
         if (!Settings.Enabled)
         {
            Console.WriteLine("Email service is not enabled");
            return false;
         }

         var mailMessage = new MimeMessage();
         mailMessage.From.Add(MailboxAddress.Parse(Settings.From));
         mailMessage.To.Add(MailboxAddress.Parse(recipient.Value));
         mailMessage.Subject = subject;
         mailMessage.Body = new TextPart("plain")
         {
            Text = message
         };

         using var smtpClient = new SmtpClient();

         try
         {
            await smtpClient.ConnectAsync(Settings.Host, Settings.Port, SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(Settings.Username, Settings.Password);
            await smtpClient.SendAsync(mailMessage);
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
            return false;
         }
         finally
         {
            await smtpClient.DisconnectAsync(true);
         }
         return true;
      }

      /// <summary>
      /// Send a verification email to the provided recipient.
      /// </summary>
      /// <param name="emailAddress"></param>
      /// <param name="verificationCode"></param>
      /// <param name="ecdsaPrivateKey"></param>
      /// <remarks>This method is 'virtual' to enable some unit tests.</remarks>
      /// <returns></returns>
      public virtual async Task<bool> SendEmailVerificationAsync(EmailAddress emailAddress, Guid verificationCode, AsymmetricKeyParameter ecdsaPrivateKey)
      {
         var codeBytes = verificationCode.ToByteArray();

         var signer = new ECDSA();
         signer.InitializeSigner(ecdsaPrivateKey);
         signer.SignerDigestPart(codeBytes);
         var signature = signer.GenerateSignature();

         var encodedVerificationCode = EmailVerificationEncoder.EncodeVerificationCodeUrlSafe(verificationCode);
         var encodedSignature = EmailVerificationEncoder.EncodeSignatureUrlSafe(signature);
         var verificationLink = $"https://www.crypter.dev/verify?code={encodedVerificationCode}&signature={encodedSignature}";

         return await SendAsync("Verify your email address", verificationLink, emailAddress);
      }

      /// <summary>
      /// Send a verification email using Hangfire best practices.
      /// </summary>
      /// <param name="userId"></param>
      /// <remarks>
      /// See: https://docs.hangfire.io/en/latest/best-practices.html
      /// </remarks>
      /// <returns></returns>
      public async Task HangfireSendEmailVerificationAsync(Guid userId)
      {
         var userEntity = await UserService.ReadAsync(userId, CancellationToken.None);
         var userEmailVerificationEntity = await UserEmailVerificationService.ReadAsync(userId, CancellationToken.None);

         if (userEntity == null                                         // User does not exist
            || userEntity.EmailVerified                                 // User's email address is already verified
            || userEmailVerificationEntity != null)                     // User already has a UserEmailVerification entity
         {
            return;
         }

         if (!EmailAddress.TryFrom(userEntity.Email, out var emailAddress))
         {
            return;
         }

         var verificationCode = Guid.NewGuid();
         var keys = ECDSA.GenerateKeys();

         var success = await SendEmailVerificationAsync(emailAddress, verificationCode, keys.Private);
         if (success)
         {
            byte[] verificationKey = Encoding.UTF8.GetBytes(keys.Public.ConvertToPEM().Value);
            await UserEmailVerificationService.InsertAsync(userId, verificationCode, verificationKey, CancellationToken.None);
         }
      }

      public async Task<bool> SendTransferNotificationAsync(EmailAddress emailAddress)
      {
         return await SendAsync("Someone sent you a transfer", "Someone sent you something on Crypter!  Login to https://www.crypter.dev see what it is.", emailAddress);
      }

      public async Task HangfireSendTransferNotificationAsync(TransferItemType itemType, Guid itemId)
      {
         Guid recipientId;

         switch (itemType)
         {
            case TransferItemType.Message:
               var message = await MessageTransferService.ReadAsync(itemId, CancellationToken.None);
               if (message is null)
               {
                  return;
               }

               recipientId = message.Recipient;
               break;
            case TransferItemType.File:
               var file = await FileTransferService.ReadAsync(itemId, CancellationToken.None);
               if (file is null)
               {
                  return;
               }

               recipientId = file.Recipient;
               break;
            default:
               return;
         }

         var user = await UserService.ReadAsync(recipientId, CancellationToken.None);
         if (user is null || !user.EmailVerified)
         {
            return;
         }

         if (!EmailAddress.TryFrom(user.Email, out var emailAddress))
         {
            return;
         }

         var userNotification = await UserNotificationSettingService.ReadAsync(recipientId, CancellationToken.None);
         if (userNotification is null
            || !userNotification.EnableTransferNotifications
            || !userNotification.EmailNotifications)
         {
            return;
         }

         await SendTransferNotificationAsync(emailAddress);
      }
   }
}
