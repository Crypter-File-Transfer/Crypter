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

using Crypter.Core.Identity;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Hangfire;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;

namespace Crypter.Test.Core_Tests.Services_Tests.Helpers
{
   internal static class ServiceTestFactory
   {
      internal static IOptions<EmailSettings> GetDefaultEmailOptions()
      {
         EmailSettings settings = new EmailSettings();
         return Options.Create(settings);
      }

      internal static EmailService GetEmailService(EmailSettings settings = null)
      {
         IOptions<EmailSettings> options = settings is null
            ? GetDefaultEmailOptions()
            : Options.Create(settings);

         return new EmailService(options);
      }

      internal static IOptions<TransferStorageSettings> GetDefaultTransferStorageOptions()
      {
         TransferStorageSettings transferStorageSettings = new TransferStorageSettings();
         return Options.Create(transferStorageSettings);
      }

      internal static TransferStorageService GetTransferStorageService(TransferStorageSettings settings = null)
      {
         IOptions<TransferStorageSettings> options = settings is null
            ? GetDefaultTransferStorageOptions()
            : Options.Create(settings);

         return new TransferStorageService(options);
      }

      internal static IOptions<TokenSettings> GetDefaultTokenOptions()
      {
         TokenSettings settings = new TokenSettings
         {
            Audience = "The Fellowship",
            Issuer = "Legolas",
            SecretKey = "They're taking the hobbits to Isengard!",
            AuthenticationTokenLifetimeMinutes = 5,
            SessionTokenLifetimeMinutes = 30,
            DeviceTokenLifetimeDays = 5
         };
         return Options.Create(settings);
      }

      internal static TokenService GetTokenService(TokenSettings settings = null)
      {
         IOptions<TokenSettings> options = settings is null
            ? GetDefaultTokenOptions()
            : Options.Create(settings);

         return new TokenService(options);
      }

      internal static PasswordHashService GetPasswordHashService()
      {
         return new PasswordHashService();
      }

      internal static UserEmailVerificationService GetUserEmailVerificationService(TestDataContext context)
      {
         return new UserEmailVerificationService(context);
      }

      internal static UserService GetUserService(TestDataContext context)
      {
         return new UserService(context);
      }

      /*
      internal static HangfireBackgroundService GetHangfireBackgroundService(
         TestDataContext context,
         IUserService userService = null,
         IUserEmailVerificationService userEmailVerificationService = null,
         IEmailService emailService = null,
         ITransferStorageService transferStorageService = null)
      {
         return new HangfireBackgroundService(
            context,
            userService ?? GetUserService(context),
            userEmailVerificationService ?? GetUserEmailVerificationService(context),
            emailService ?? GetEmailService(),
            transferStorageService ?? GetTransferStorageService());
      }
      */

      internal static ServerPasswordSettings GetPasswordSettings(int serverVersion = 0, int clientVersion = 0)
      {
         PasswordVersion version = new PasswordVersion
         {
            Version = serverVersion,
            Algorithm = "foo",
            Iterations = 1
         };

         return new ServerPasswordSettings()
         {
            ClientVersion = clientVersion,
            ServerVersions = new List<PasswordVersion> { version }
         };
      }

      internal static UserAuthenticationService GetUserAuthenticationService(TestDataContext context, ServerPasswordSettings settings = null)
      {
         var passwordHashService = GetPasswordHashService();
         var tokenService = GetTokenService();
         var backgroundJobClient = new Mock<IBackgroundJobClient>();
         var hangfireBackgroundService = new Mock<IHangfireBackgroundService>();

         return new UserAuthenticationService(context, passwordHashService, tokenService, backgroundJobClient.Object, hangfireBackgroundService.Object, settings ?? GetPasswordSettings());
      }
   }
}
