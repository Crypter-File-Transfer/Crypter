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
using Crypter.Contracts.Features.User.Register;
using Crypter.Web.Models.Forms;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class RegisterComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ILocalStorageService LocalStorageService { get; set; }

      [Inject]
      protected IUserApiService UserService { get; set; }

      protected UserRegistrationModel RegistrationInfo = new();

      protected bool RegistrationError = false;
      protected string RegistrationErrorText = "";
      protected bool RegistrationSuccess = false;

      protected string IsInvalid = "is-invalid";

      protected string UsernameInvalidClass = "";
      protected string UsernameValidationMessage;
      private readonly static string MissingUsername = "Please choose a username";
      private readonly static string UsernameTooLong = "Username exceeds 32-character limit";
      private readonly static string UsernameInvalidCharacters = "Username contains invalid character(s)";

      protected string PasswordInvalidClass = "";
      protected string PasswordValidationMessage;
      private readonly static string MissingPassword = "Please enter a password";

      protected string PasswordConfirmInvalidClass = "";
      protected string PasswordConfirmValidationMessage;
      private readonly static string MissingPasswordConfirm = "Please confirm your password";
      private readonly static string PasswordConfirmDoesNotMatch = "Passwords do not match";

      protected bool UserProvidedEmailDuringRegistration = false;

      protected override async Task OnInitializedAsync()
      {
         if (LocalStorageService.HasItem(StoredObjectType.UserSession))
         {
            NavigationManager.NavigateTo("/user");
         }
         await base.OnInitializedAsync();
      }

      private bool ValidateForm()
      {
         return ValidateUsername()
            && ValidatePassword()
            && ValidatePasswordConfirmation();
      }

      private bool ValidateUsername()
      {
         if (string.IsNullOrEmpty(RegistrationInfo.Username))
         {
            UsernameValidationMessage = MissingUsername;
            UsernameInvalidClass = IsInvalid;
            return false;
         }
         else if (!ValidationService.UsernameMeetsLengthRequirements(RegistrationInfo.Username))
         {
            UsernameValidationMessage = UsernameTooLong;
            UsernameInvalidClass = IsInvalid;
            return false;
         }
         else if (!ValidationService.UsernameMeetsCharacterRequirements(RegistrationInfo.Username))
         {
            UsernameValidationMessage = UsernameInvalidCharacters;
            UsernameInvalidClass = IsInvalid;
            return false;
         }

         UsernameInvalidClass = "";
         return true;
      }

      private bool ValidatePassword()
      {
         if (string.IsNullOrEmpty(RegistrationInfo.Password))
         {
            PasswordValidationMessage = MissingPassword;
            PasswordInvalidClass = IsInvalid;
            return false;
         }

         PasswordInvalidClass = "";
         return true;
      }

      private bool ValidatePasswordConfirmation()
      {
         if (string.IsNullOrEmpty(RegistrationInfo.PasswordConfirm))
         {
            PasswordConfirmValidationMessage = MissingPasswordConfirm;
            PasswordConfirmInvalidClass = IsInvalid;
            return false;
         }
         else if (!RegistrationInfo.Password.Equals(RegistrationInfo.PasswordConfirm))
         {
            PasswordConfirmValidationMessage = PasswordConfirmDoesNotMatch;
            PasswordConfirmInvalidClass = IsInvalid;
            return false;
         }

         PasswordConfirmInvalidClass = "";
         return true;
      }

      protected async Task OnRegisterClickedAsync()
      {
         if (!ValidateForm())
         {
            return;
         }

         byte[] digestedPassword = CryptoLib.UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(RegistrationInfo.Username, RegistrationInfo.Password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var requestBody = new UserRegisterRequest(RegistrationInfo.Username, digestedPasswordBase64, RegistrationInfo.EmailAddress);
         var maybeRegistration = await UserService.RegisterUserAsync(requestBody);
         RegistrationSuccess = maybeRegistration.Match(
            left =>
            {
               RegistrationError = true;
               RegistrationErrorText = (UserRegisterResult)left.ErrorCode switch
               {
                  UserRegisterResult.InvalidUsername => "Invalid username",
                  UserRegisterResult.InvalidPassword => "Invalid password",
                  UserRegisterResult.InvalidEmailAddress => "Invalid email address",
                  UserRegisterResult.UsernameTaken => "Username is already taken",
                  UserRegisterResult.EmailTaken => "Email address is associated with an existing account",
                  _ => "???"
               };
               return false;
            },
            right =>
            {
               UserProvidedEmailDuringRegistration = !string.IsNullOrEmpty(RegistrationInfo.EmailAddress);
               return true;
            });
      }
   }
}
