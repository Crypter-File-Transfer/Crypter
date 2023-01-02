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

using Crypter.ClientServices.Interfaces;
using Crypter.Common.Contracts.Features.Authentication;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Common.Primitives.Enums;
using Crypter.Web.Models.Forms;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class RegisterComponentBase : ComponentBase
   {
      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      [Inject]
      protected IUserSessionService UserSessionService { get; set; }

      [Inject]
      protected IUserPasswordService UserPasswordService { get; set; }

      protected const string _invalidClassName = "is-invalid";

      protected UserRegistrationForm RegistrationModel;

      protected bool RegistrationAttemptFailed;
      protected bool RegistrationAttemptSucceeded;
      protected string RegistrationAttemptErrorMessage;

      protected string UsernameInvalidClassPlaceholder;
      protected string UsernameValidationErrorMessage;

      protected string PasswordInvalidClassPlaceholder;
      protected string PasswordValidationErrorMessage;

      protected string PasswordConfirmInvalidClassPlaceholder;
      protected string PasswordCondirmValidationErrorMessage;

      protected string EmailAddressInvalidClassPlaceholder;
      protected string EmailAddressValidationErrorMessage;

      protected bool UserProvidedEmailAddress;

      protected override void OnInitialized()
      {
         RegistrationModel = new();
      }

      private Maybe<Username> ValidateUsername()
      {
         var invalidReason = Username.CheckValidation(RegistrationModel.Username);

         invalidReason.IfNone(() =>
         {
            UsernameInvalidClassPlaceholder = "";
            UsernameValidationErrorMessage = "";
         });

         invalidReason.IfSome(error =>
         {
            UsernameInvalidClassPlaceholder = _invalidClassName;
            UsernameValidationErrorMessage = error switch
            {
               StringPrimitiveValidationFailure.IsNull
                  or StringPrimitiveValidationFailure.IsEmpty => "Please choose a username",
               StringPrimitiveValidationFailure.TooLong => "Username exceeds 32-character limit",
               StringPrimitiveValidationFailure.InvalidCharacters => "Username contains invalid character(s)",
               _ => "Invalid username"
            };
         });

         return invalidReason.Match(
            () => Username.From(RegistrationModel.Username),
            _ => Maybe<Username>.None);
      }

      private Maybe<Password> ValidatePassword()
      {
         var invalidReason = Password.CheckValidation(RegistrationModel.Password);

         invalidReason.IfNone(() =>
         {
            PasswordInvalidClassPlaceholder = "";
            PasswordValidationErrorMessage = "";
         });

         invalidReason.IfSome(error =>
         {
            PasswordInvalidClassPlaceholder = _invalidClassName;
            PasswordValidationErrorMessage = error switch
            {
               StringPrimitiveValidationFailure.IsNull
                  or StringPrimitiveValidationFailure.IsEmpty => "Please enter a password",
               _ => "Invalid password"
            };
         });

         return invalidReason.Match(
            () => Password.From(RegistrationModel.Password),
            _ => Maybe<Password>.None);
      }

      private bool ValidatePasswordConfirmation()
      {
         bool passwordConfirmMissing = string.IsNullOrEmpty(RegistrationModel.PasswordConfirm);
         if (passwordConfirmMissing)
         {
            PasswordConfirmInvalidClassPlaceholder = _invalidClassName;
            PasswordCondirmValidationErrorMessage = "Please confirm your password";
            return false;
         }

         bool passwordsMatch = RegistrationModel.Password == RegistrationModel.PasswordConfirm;
         if (!passwordsMatch)
         {
            PasswordConfirmInvalidClassPlaceholder = _invalidClassName;
            PasswordCondirmValidationErrorMessage = "Passwords do not match";
            return false;
         }

         PasswordConfirmInvalidClassPlaceholder = "";
         PasswordCondirmValidationErrorMessage = "";
         return true;
      }

      private Either<Unit, Maybe<EmailAddress>> ValidateEmailAddress()
      {
         bool isEmailAddressEmpty = string.IsNullOrEmpty(RegistrationModel.EmailAddress);
         if (isEmailAddressEmpty)
         {
            return Maybe<EmailAddress>.None;
         }

         var invalidReason = EmailAddress.CheckValidation(RegistrationModel.EmailAddress);

         invalidReason.IfNone(() =>
         {
            EmailAddressInvalidClassPlaceholder = "";
            EmailAddressValidationErrorMessage = "";
         });

         invalidReason.IfSome(error =>
         {
            EmailAddressInvalidClassPlaceholder = _invalidClassName;
            EmailAddressValidationErrorMessage = "Invalid email address";
         });

         return invalidReason.Match<Either<Unit, Maybe<EmailAddress>>>(
            () => Maybe<EmailAddress>.From(EmailAddress.From(RegistrationModel.EmailAddress)),
            _ => Unit.Default);
      }

      protected async Task SubmitRegistrationAsync()
      {
         var registrationResult = await ValidateUsername()
            .BindAsync(username => ValidatePassword()
            .MapAsync(password => ValidateEmailAddress().MapLeft(_ => RegistrationError.InvalidEmailAddress)
            .BindAsync(async emailAddress =>
            {
               UserProvidedEmailAddress = emailAddress.IsSome;

               if (!ValidatePasswordConfirmation())
               {
                  return RegistrationError.InvalidPasswordConfirm;
               }

               return await UserPasswordService.DeriveUserAuthenticationPasswordAsync(username, password, UserPasswordService.CurrentPasswordVersion)
                  .ToEitherAsync(RegistrationError.PasswordHashFailure)
                  .BindAsync(async versionedPassword =>
                  {
                     RegistrationRequest request = new RegistrationRequest(username, versionedPassword, emailAddress);
                     return await CrypterApiService.RegisterUserAsync(request);
                  });
            })));

         registrationResult.IfSome(x => x.DoLeftOrNeither(HandleRegistrationFailure, HandleUnknownRegistrationFailure));
         RegistrationAttemptSucceeded = registrationResult.Match(
            () => false,
            x => x.IsRight
         );
      }

      private void HandleRegistrationFailure(RegistrationError error)
      {
         RegistrationAttemptFailed = error != RegistrationError.InvalidPasswordConfirm;
#pragma warning disable CS8524
         RegistrationAttemptErrorMessage = error switch
         {
            RegistrationError.InvalidUsername => "Invalid username",
            RegistrationError.InvalidPassword => "Invalid password",
            RegistrationError.InvalidEmailAddress => "Invalid email address",
            RegistrationError.UsernameTaken => "Username is already taken",
            RegistrationError.EmailAddressTaken => "Email address is associated with an existing account",
            RegistrationError.PasswordHashFailure => "A cryptographic error occurred. This device or browser may not be supported.",
            RegistrationError.OldPasswordVersion
               or RegistrationError.UnknownError => "An unknown error ocurred.",
            RegistrationError.InvalidPasswordConfirm => string.Empty
         };
#pragma warning restore CS8524
      }

      private void HandleUnknownRegistrationFailure()
      {
         RegistrationAttemptFailed = true;
         RegistrationAttemptErrorMessage = "An unknown error occurred.";
      }
   }
}
