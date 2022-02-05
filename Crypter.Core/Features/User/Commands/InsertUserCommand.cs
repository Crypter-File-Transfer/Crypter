/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Common.Services;
using Crypter.Contracts.Common.Enum;
using Crypter.Contracts.Features.User.Register;
using Crypter.Core.Features.User.Common;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Commands
{
   public class InsertUserCommand : IRequest<InsertUserCommandResult>
   {
      public string Username { get; private set; }
      public string PasswordBase64 { get; private set; }
      public string? Email { get; private set; }

      public InsertUserCommand(string username, string passwordBase64, string? email)
      {
         Username = username;
         PasswordBase64 = passwordBase64;
         Email = email;
      }
   }

   public class InsertUserCommandResult
   {
      public UserRegisterResult Result { get; private set; }
      public Guid UserId { get; private set; }
      public bool SendVerificationEmail { get; private set; }

      /// <summary>
      /// Failure
      /// </summary>
      /// <param name="failureReason"></param>
      public InsertUserCommandResult(UserRegisterResult failureReason)
      {
         Result = failureReason;
         UserId = Guid.Empty;
         SendVerificationEmail = false;
      }

      /// <summary>
      /// Success
      /// </summary>
      /// <param name="userId"></param>
      /// <param name="sendVerificationEmail"></param>
      public InsertUserCommandResult(Guid userId, bool sendVerificationEmail)
      {
         Result = UserRegisterResult.Success;
         UserId = userId;
         SendVerificationEmail = sendVerificationEmail;
      }
   }

   public class InsertUserCommandHandler : IRequestHandler<InsertUserCommand, InsertUserCommandResult>
   {
      private readonly DataContext _context;
      private readonly IPasswordHashService _passwordHashService;

      public InsertUserCommandHandler(DataContext context, IPasswordHashService passwordHashService)
      {
         _context = context;
         _passwordHashService = passwordHashService;
      }

      public async Task<InsertUserCommandResult> Handle(InsertUserCommand request, CancellationToken cancellationToken)
      {
         if (!ValidationService.IsValidUsername(request.Username))
         {
            return new(UserRegisterResult.InvalidUsername);
         }

         if (!ValidationService.IsValidPassword(request.PasswordBase64))
         {
            return new(UserRegisterResult.InvalidPassword);
         }

         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && !ValidationService.IsValidEmailAddress(request.Email))
         {
            return new(UserRegisterResult.InvalidEmailAddress);
         }

         bool isUsernameAvailable = await _context.Users.IsUsernameAvailableAsync(request.Username, cancellationToken);
         if (!isUsernameAvailable)
         {
            return new(UserRegisterResult.UsernameTaken);
         }

         bool sendVerificationEmail = false;
         if (ValidationService.IsPossibleEmailAddress(request.Email))
         {
            bool isEmailAvailable = await _context.Users.IsEmailAddressAvailableAsync(request.Email, cancellationToken);
            if (!isEmailAvailable)
            {
               return new(UserRegisterResult.EmailTaken);
            }
            sendVerificationEmail = true;
         }

         (byte[] passwordSalt, byte[] passwordHash) = _passwordHashService.MakeSecurePasswordHash(request.PasswordBase64);

         Models.User user = new(Guid.NewGuid(), request.Username, request.Email, passwordHash, passwordSalt, false, DateTime.UtcNow, DateTime.MinValue);
         user.Profile = new UserProfile(user.Id, null, null, null);
         user.PrivacySetting = new UserPrivacySetting(user.Id, true, UserVisibilityLevel.Everyone, UserItemTransferPermission.Everyone, UserItemTransferPermission.Everyone);
         user.NotificationSetting = new UserNotificationSetting(user.Id, false, false);

         _context.Users.Add(user);
         await _context.SaveChangesAsync(cancellationToken);

         return new InsertUserCommandResult(user.Id, sendVerificationEmail);
      }
   }
}
