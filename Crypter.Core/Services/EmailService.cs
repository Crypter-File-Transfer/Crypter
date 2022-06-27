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
using Crypter.Core.Models;
using Crypter.Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IEmailService
   {
      Task<bool> SendAsync(string subject, string message, EmailAddress recipient, CancellationToken cancellationToken);
      Task<bool> SendEmailVerificationAsync(UserEmailAddressVerificationParameters parameters, CancellationToken cancellationToken);
      Task<bool> SendTransferNotificationAsync(EmailAddress emailAddress, CancellationToken cancellationToken);
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
      public virtual async Task<bool> SendAsync(string subject, string message, EmailAddress recipient, CancellationToken cancellationToken)
      {
         if (!Settings.Enabled)
         {
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
            await smtpClient.ConnectAsync(Settings.Host, Settings.Port, SecureSocketOptions.StartTls, cancellationToken);
            await smtpClient.AuthenticateAsync(Settings.Username, Settings.Password, cancellationToken);
            await smtpClient.SendAsync(mailMessage, cancellationToken);
         }
         catch (Exception ex)
         {
            Console.WriteLine(ex.Message);
            return false;
         }
         finally
         {
            await smtpClient.DisconnectAsync(true, cancellationToken);
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
      public virtual async Task<bool> SendEmailVerificationAsync(UserEmailAddressVerificationParameters parameters, CancellationToken cancellationToken)
      {
         var encodedVerificationCode = EmailVerificationEncoder.EncodeVerificationCodeUrlSafe(parameters.VerificationCode);
         var encodedSignature = EmailVerificationEncoder.EncodeSignatureUrlSafe(parameters.Signature);
         var verificationLink = $"https://www.crypter.dev/verify?code={encodedVerificationCode}&signature={encodedSignature}";

         return await SendAsync("Verify your email address", verificationLink, parameters.EmailAddress, cancellationToken);
      }

      public async Task<bool> SendTransferNotificationAsync(EmailAddress emailAddress, CancellationToken cancellationToken)
      {
         return await SendAsync("Someone sent you a transfer", "Someone sent you something on Crypter!  Login to https://www.crypter.dev see what it is.", emailAddress, cancellationToken);
      }
   }
}
