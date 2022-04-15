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

using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
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
   public class InsertUserCommand : IRequest<Either<UserRegisterError, InsertUserCommandResult>>
   {
      public Username Username { get; init; }
      public AuthenticationPassword Password { get; init; }
      public Maybe<EmailAddress> EmailAddress { get; init; }

      public InsertUserCommand(Username username, AuthenticationPassword password, Maybe<EmailAddress> emailAddress)
      {
         Username = username;
         Password = password;
         EmailAddress = emailAddress;
      }

      public static Either<UserRegisterError, InsertUserCommand> ValidateFrom(string username, string password, string emailAddress)
      {
         if (!Username.TryFrom(username, out var validUsername))
         {
            return UserRegisterError.InvalidUsername;
         }

         if (!AuthenticationPassword.TryFrom(password, out var validAuthenticationPassword))
         {
            return UserRegisterError.InvalidPassword;
         }

         bool isPossibleEmailAddress = !string.IsNullOrEmpty(emailAddress);
         Maybe<EmailAddress> validatedEmailAddress = Crypter.Common.Primitives.EmailAddress.TryFrom(emailAddress, out var validEmailAddressOrNull)
            ? validEmailAddressOrNull
            : Maybe<EmailAddress>.None;

         if (isPossibleEmailAddress && validatedEmailAddress.IsNone)
         {
            return UserRegisterError.InvalidEmailAddress;
         }

         return new InsertUserCommand(validUsername, validAuthenticationPassword, validatedEmailAddress);
      }
   }

   public class InsertUserCommandResult
   {
      public Guid UserId { get; private set; }
      public bool SendVerificationEmail { get; private set; }

      public InsertUserCommandResult(Guid userId, bool sendVerificationEmail)
      {
         UserId = userId;
         SendVerificationEmail = sendVerificationEmail;
      }
   }

   public class InsertUserCommandHandler : IRequestHandler<InsertUserCommand, Either<UserRegisterError, InsertUserCommandResult>>
   {
      private readonly DataContext _context;
      private readonly IPasswordHashService _passwordHashService;

      public InsertUserCommandHandler(DataContext context, IPasswordHashService passwordHashService)
      {
         _context = context;
         _passwordHashService = passwordHashService;
      }

      public async Task<Either<UserRegisterError, InsertUserCommandResult>> Handle(InsertUserCommand request, CancellationToken cancellationToken)
      {
         bool isUsernameAvailable = await _context.Users.IsUsernameAvailableAsync(request.Username, cancellationToken);
         if (!isUsernameAvailable)
         {
            return UserRegisterError.UsernameTaken;
         }

         if (request.EmailAddress.IsSome)
         {
            bool isEmailAddressAvailable = await _context.Users.IsEmailAddressAvailableAsync(request.EmailAddress.ValueUnsafe, cancellationToken);
            if (!isEmailAddressAvailable)
            {
               return UserRegisterError.EmailTaken;
            }
         }

         bool sendVerificationEmail = request.EmailAddress.IsSome;

         (byte[] passwordSalt, byte[] passwordHash) = _passwordHashService.MakeSecurePasswordHash(request.Password);

         Models.User user = new(Guid.NewGuid(), request.Username, request.EmailAddress, passwordHash, passwordSalt, false, DateTime.UtcNow, DateTime.MinValue);
         user.Profile = new UserProfile(user.Id, null, null, null);
         user.PrivacySetting = new UserPrivacySetting(user.Id, true, UserVisibilityLevel.Everyone, UserItemTransferPermission.Everyone, UserItemTransferPermission.Everyone);
         user.NotificationSetting = new UserNotificationSetting(user.Id, false, false);

         _context.Users.Add(user);
         await _context.SaveChangesAsync(cancellationToken);

         return new InsertUserCommandResult(user.Id, sendVerificationEmail);
      }
   }
}
