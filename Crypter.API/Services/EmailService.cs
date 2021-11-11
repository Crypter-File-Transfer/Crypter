using Crypter.API.Controllers.Methods;
using Crypter.API.Models;
using Crypter.Common.Services;
using Crypter.Core.Interfaces;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Org.BouncyCastle.Crypto;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public interface IEmailService
   {
      Task<bool> SendAsync(string subject, string message, string recipient);
      Task<bool> SendEmailVerificationAsync(string emailAddress, Guid verificationCode, AsymmetricKeyParameter ecdsaPrivateKey);
      Task HangfireSendEmailVerificationAsync(Guid userId);
   }

   public class EmailService : IEmailService
   {
      private readonly EmailSettings Settings;

      private readonly IUserService UserService;
      private readonly IUserEmailVerificationService UserEmailVerificationService;

      public EmailService(IConfiguration configuration, IUserService userService, IUserEmailVerificationService userEmailVerificationService)
      {
         UserService = userService;
         UserEmailVerificationService = userEmailVerificationService;

         Settings = new()
         {
            Enabled = configuration.GetValue<bool>("EmailSettings:Enabled"),
            From = configuration.GetValue<string>("EmailSettings:From"),
            Username = configuration.GetValue<string>("EmailSettings:Username"),
            Password = configuration.GetValue<string>("EmailSettings:Password"),
            Host = configuration.GetValue<string>("EmailSettings:Host"),
            Port = configuration.GetValue<int>("EmailSettings:Port")
         };
      }

      /// <summary>
      /// Send an email to the provided recipient.
      /// </summary>
      /// <param name="subject"></param>
      /// <param name="message"></param>
      /// <param name="recipient"></param>
      /// <remarks>This method is 'virtual' to enable some unit tests.</remarks>
      /// <returns></returns>
      public virtual async Task<bool> SendAsync(string subject, string message, string recipient)
      {
         if (!Settings.Enabled)
         {
            Console.WriteLine("Email service is not enabled");
            return false;
         }

         if (!ValidationService.IsValidEmailAddress(recipient))
         {
            return false;
         }

         var mailMessage = new MimeMessage();
         mailMessage.From.Add(MailboxAddress.Parse(Settings.From));
         mailMessage.To.Add(MailboxAddress.Parse(recipient));
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
      public virtual async Task<bool> SendEmailVerificationAsync(string emailAddress, Guid verificationCode, AsymmetricKeyParameter ecdsaPrivateKey)
      {
         var codeBytes = verificationCode.ToByteArray();

         var signer = new ECDSA();
         signer.InitializeSigner(ecdsaPrivateKey);
         signer.SignerDigestChunk(codeBytes);
         var signature = signer.GenerateSignature();

         var encodedVerificationCode = EmailVerificationEncoder.EncodeVerificationCodeUrlSafe(verificationCode);
         var encodedSignature = EmailVerificationEncoder.EncodeSignatureUrlSafe(signature);
         var verificationLink = $"https://crypter.dev/verify?code={encodedVerificationCode}&signature={encodedSignature}";

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
         var userEntity = await UserService.ReadAsync(userId);
         var userEmailVerificationEntity = await UserEmailVerificationService.ReadAsync(userId);

         if (userEntity == null                                         // User does not exist
            || !ValidationService.IsValidEmailAddress(userEntity.Email)  // User does not have a valid email address
            || userEntity.EmailVerified                                 // User's email address is already verified
            || userEmailVerificationEntity != null)                     // User already has a UserEmailVerification entity
         {
            return;
         }

         var verificationCode = Guid.NewGuid();
         var keys = ECDSA.GenerateKeys();

         var success = await SendEmailVerificationAsync(userEntity.Email, verificationCode, keys.Private);
         if (success)
         {
            await UserEmailVerificationService.InsertAsync(userId, verificationCode, Encoding.UTF8.GetBytes(keys.Public.ConvertToPEM()));
         }
      }
   }
}
