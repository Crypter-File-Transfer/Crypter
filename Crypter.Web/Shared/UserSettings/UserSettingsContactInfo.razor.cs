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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;
using Crypter.Common.Primitives;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.UserSettings
{
   public partial class UserSettingsContactInfoBase : ComponentBase
   {
      [Inject]
      protected IUserContactInfoSettingsService UserContactInfoSettingsService { get; set; }

      protected string EmailAddress { get; set; } = string.Empty;
      protected string EmailAddressEdit { get; set; } = string.Empty;

      protected bool EmailAddressVerified { get; set; } = false;

      protected string Password { get; set; } = string.Empty;

      protected bool IsDataReady { get; set; } = false;
      protected bool IsEditing { get; set; } = false;

      protected string EmailAddressError = string.Empty;
      protected string PasswordError = string.Empty;
      protected string GenericError = string.Empty;

      protected override async Task OnInitializedAsync()
      {
         await UserContactInfoSettingsService.GetContactInfoSettingsAsync()
            .IfSomeAsync(x =>
            {
               EmailAddress = x.EmailAddress;
               EmailAddressEdit = x.EmailAddress;

               EmailAddressVerified = x.EmailAddressVerified;
            });

         IsDataReady = true;
      }

      protected void OnEditClicked()
      {
         IsEditing = true;
      }

      protected void OnCancelClicked()
      {
         ResetErrors();
         Password = string.Empty;
         EmailAddressEdit = EmailAddress;
         IsEditing = false;
      }

      private void ResetErrors()
      {
         EmailAddressError = string.Empty;
         PasswordError = string.Empty;
         GenericError = string.Empty;
      }

      protected async Task OnSaveClickedAsync()
      {
         ResetErrors();

         if (!Common.Primitives.Password.TryFrom(Password, out var password))
         {
            PasswordError = "Enter your current password";
            return;
         }

         bool someEmailAddress = !string.IsNullOrEmpty(EmailAddressEdit);
         bool validEmailAddress = Common.Primitives.EmailAddress.TryFrom(EmailAddressEdit, out var emailAddress);

         if (someEmailAddress && !validEmailAddress)
         {
            EmailAddressError = "You must either enter a valid email address or provide a blank value";
            return;
         }

         Maybe<EmailAddress> newEmailAddress = validEmailAddress
            ? emailAddress
            : Maybe<EmailAddress>.None;

         await UserContactInfoSettingsService.UpdateContactInfoSettingsAsync(emailAddress, password)
            .DoRightAsync(x =>
            {
               EmailAddress = x.EmailAddress;
               EmailAddressEdit = x.EmailAddress;

               EmailAddressVerified = x.EmailAddressVerified;
            })
            .DoLeftOrNeitherAsync(
               HandleContactInfoUpdateError,
               () => HandleContactInfoUpdateError());

         Password = string.Empty;
         IsEditing = false;
      }

      private void HandleContactInfoUpdateError(UpdateContactInfoSettingsError error = UpdateContactInfoSettingsError.UnknownError)
      {
         switch (error)
         {
            case UpdateContactInfoSettingsError.UnknownError:
            case UpdateContactInfoSettingsError.UserNotFound:
               GenericError = "An error occurred";
               break;
            case UpdateContactInfoSettingsError.EmailAddressUnavailable:
               EmailAddressError = "Email address unavailable";
               break;
            case UpdateContactInfoSettingsError.InvalidEmailAddress:
               EmailAddressError = "Invalid email address";
               break;
            case UpdateContactInfoSettingsError.InvalidPassword:
               PasswordError = "Incorrect password";
               break;
            case UpdateContactInfoSettingsError.PasswordHashFailure:
               PasswordError = "A cryptographic error occured. This device or browser may not be supported.";
               break;
         }
      }
   }
}
