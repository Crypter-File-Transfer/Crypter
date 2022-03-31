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
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Common.Primitives.Enums;
using Crypter.Contracts.Features.User.Register;
using Crypter.Web.Models.Forms;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class RegisterComponentBase : ComponentBase
   {
      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

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

      private Either<Nothing, Username> ValidateUsername()
      {
         var validationResult = Username.CheckValidation(RegistrationModel.Username);

         validationResult.IfNone(() =>
         {
            UsernameInvalidClassPlaceholder = "";
            UsernameValidationErrorMessage = "";
         });

         validationResult.IfSome(error =>
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

         return validationResult.Match<Either<Nothing, Username>>(
            () => Username.From(RegistrationModel.Username),
            _ => new Nothing());
      }

      private Either<Nothing, Password> ValidatePassword()
      {
         var validationResult = Password.CheckValidation(RegistrationModel.Password);

         validationResult.IfNone(() =>
         {
            PasswordInvalidClassPlaceholder = "";
            PasswordValidationErrorMessage = "";
         });

         validationResult.IfSome(error =>
         {
            PasswordInvalidClassPlaceholder = _invalidClassName;
            PasswordValidationErrorMessage = error switch
            {
               StringPrimitiveValidationFailure.IsNull
                  or StringPrimitiveValidationFailure.IsEmpty => "Please enter a password",
               _ => "Invalid password"
            };
         });

         return validationResult.Match<Either<Nothing, Password>>(
            () => Password.From(RegistrationModel.Password),
            _ => new Nothing());
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

      private Either<Nothing, Maybe<EmailAddress>> ValidateEmailAddress()
      {
         bool isEmailAddressEmpty = string.IsNullOrEmpty(RegistrationModel.EmailAddress);
         if (isEmailAddressEmpty)
         {
            return Maybe<EmailAddress>.None;
         }

         var validationResult = EmailAddress.CheckValidation(RegistrationModel.EmailAddress);

         validationResult.IfNone(() =>
         {
            EmailAddressInvalidClassPlaceholder = "";
            EmailAddressValidationErrorMessage = "";
         });

         validationResult.IfSome(error =>
         {
            EmailAddressInvalidClassPlaceholder = _invalidClassName;
            EmailAddressValidationErrorMessage = "Invalid email address";
         });

         return validationResult.Match<Either<Nothing, Maybe<EmailAddress>>>(
            () => new Maybe<EmailAddress>(EmailAddress.From(RegistrationModel.EmailAddress)),
            _ => new Nothing());
      }

      protected async Task SubmitRegistrationAsync()
      {
         var maybeUsername = ValidateUsername();
         var maybePassword = ValidatePassword();
         bool passwordsMatch = ValidatePasswordConfirmation();
         var maybeEmailAddress = ValidateEmailAddress();

         if (maybeUsername.IsLeft || maybePassword.IsLeft || !passwordsMatch || maybeEmailAddress.IsLeft)
         {
            return;
         }

         var username = maybeUsername.RightOrDefault();
         var password = maybePassword.RightOrDefault();
         var emailAddress = maybeEmailAddress.RightOrDefault();

         byte[] authPasswordBytes = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
         string authPasswordEncoded = Convert.ToBase64String(authPasswordBytes);
         var authenticationPassword = AuthenticationPassword.From(authPasswordEncoded);

         var requestBody = new UserRegisterRequest(username, authenticationPassword, emailAddress);
         var registrationAttempt = await CrypterApiService.RegisterUserAsync(requestBody);

         registrationAttempt.DoLeft(HandleRegistrationFailure);
         registrationAttempt.DoRight(_ => UserProvidedEmailAddress = emailAddress.IsSome);
         RegistrationAttemptSucceeded = registrationAttempt.IsRight;
      }

      private void HandleRegistrationFailure(UserRegisterError error)
      {
         RegistrationAttemptFailed = true;
         RegistrationAttemptErrorMessage = error switch
         {
            UserRegisterError.InvalidUsername => "Invalid username",
            UserRegisterError.InvalidPassword => "Invalid password",
            UserRegisterError.InvalidEmailAddress => "Invalid email address",
            UserRegisterError.UsernameTaken => "Username is already taken",
            UserRegisterError.EmailTaken => "Email address is associated with an existing account",
            _ => "???"
         };
      }
   }
}
