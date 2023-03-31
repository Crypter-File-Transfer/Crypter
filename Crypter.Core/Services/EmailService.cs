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

using Crypter.Common.Infrastructure;
using Crypter.Common.Primitives;
using Crypter.Core.Models;
using Crypter.Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IEmailService
   {
      Task<bool> SendAsync(string subject, string message, EmailAddress recipient);
      Task<bool> SendAccountRecoveryLinkAsync(UserRecoveryParameters parameters, int expirationMinutes);
      Task<bool> SendEmailVerificationAsync(UserEmailAddressVerificationParameters parameters);
      Task<bool> SendTransferNotificationAsync(EmailAddress emailAddress);
   }

   public static class EmailServiceExtensions
   {
      public static void AddEmailService(this IServiceCollection services, Action<EmailSettings> settings)
      {
         if (settings is null)
         {
            throw new ArgumentNullException(nameof(settings));
         }

         services.Configure(settings);
         services.TryAddSingleton<IEmailService, EmailService>();
      }
   }

   public class EmailService : IEmailService
   {
      private readonly EmailSettings Settings;

      public EmailService(IOptions<EmailSettings> emailSettings)
      {
         Settings = emailSettings.Value;
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
            return true;
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

      public async Task<bool> SendAccountRecoveryLinkAsync(UserRecoveryParameters parameters, int expirationMinutes)
      {
         string encodedUsername = UrlSafeEncoder.EncodeStringUrlSafe(parameters.Username.Value);
         string encodedVerificationCode = UrlSafeEncoder.EncodeGuidUrlSafe(parameters.RecoveryCode);
         string encodedSignature = UrlSafeEncoder.EncodeBytesUrlSafe(parameters.Signature);
         string recoveryLink = $"Click the link below to begin account recovery.\n" +
            $"https://www.crypter.dev/recovery?username={encodedUsername}&code={encodedVerificationCode}&signature={encodedSignature}\n" +
            $"This link will expire in {expirationMinutes} minutes.";

         return await SendAsync("Account recovery", recoveryLink, parameters.EmailAddress);
      }

      /// <summary>
      /// Send a verification email to the provided recipient.
      /// </summary>
      /// <param name="emailAddress"></param>
      /// <param name="verificationCode"></param>
      /// <param name="ecdsaPrivateKey"></param>
      /// <remarks>This method is 'virtual' to enable some unit tests.</remarks>
      /// <returns></returns>
      public virtual async Task<bool> SendEmailVerificationAsync(UserEmailAddressVerificationParameters parameters)
      {
         string encodedVerificationCode = UrlSafeEncoder.EncodeGuidUrlSafe(parameters.VerificationCode);
         string encodedSignature = UrlSafeEncoder.EncodeBytesUrlSafe(parameters.Signature);
         string verificationLink = $"https://www.crypter.dev/verify?code={encodedVerificationCode}&signature={encodedSignature}";

         return await SendAsync("Verify your email address", verificationLink, parameters.EmailAddress);
      }

      public async Task<bool> SendTransferNotificationAsync(EmailAddress emailAddress)
      {
         return await SendAsync("Someone sent you a transfer", "Someone sent you something on Crypter!  Login to https://www.crypter.dev see what it is.", emailAddress);
      }
   }
}
