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

using Crypter.Common.Client.Interfaces;
using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Monads;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.UserSettings
{
   public partial class UserSettingsContactInfoBase : ComponentBase
   {
      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      [Inject]
      protected IUserPasswordService UserPasswordService { get; set; }

      [Parameter]
      public string Username { get; set; }

      [Parameter]
      public string EmailAddress { get; set; }

      private bool _emailAddressVerified;

      [Parameter]
      public bool EmailAddressVerified
      {
         get { return _emailAddressVerified; }
         set
         {
            if (_emailAddressVerified == value) return;
            _emailAddressVerified = value;
            EmailAddressVerifiedChanged.InvokeAsync(value);
         }
      }

      [Parameter]
      public EventCallback<bool> EmailAddressVerifiedChanged { get; set; }

      protected bool IsEditing;
      protected string EmailAddressEdit;
      protected string Password;

      protected string EmailAddressError;
      protected string PasswordError;
      protected string GenericError;

      protected override void OnParametersSet()
      {
         EmailAddressEdit = EmailAddress;
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

         Common.Primitives.Username username = Common.Primitives.Username.From(Username);

         Maybe<VersionedPassword> authPasswordResult = await UserPasswordService.DeriveUserAuthenticationPasswordAsync(username, password, UserPasswordService.CurrentPasswordVersion);
         authPasswordResult.IfNone(() => HandleContactInfoUpdateError(UpdateContactInfoError.PasswordHashFailure));

         await authPasswordResult.IfSomeAsync(async authPassword =>
         {
            var request = new UpdateContactInfoRequest(EmailAddressEdit, authPassword.Password);
            var maybeUpdate = await CrypterApiService.UpdateContactInfoAsync(request);

            maybeUpdate.DoLeftOrNeither(HandleContactInfoUpdateError, () => HandleContactInfoUpdateError());
            maybeUpdate.DoRight(right =>
            {
               EmailAddress = EmailAddressEdit;
               Password = "";
               EmailAddressVerified = false;
               IsEditing = false;
            });

         });
      }

      private void HandleContactInfoUpdateError(UpdateContactInfoError error = UpdateContactInfoError.UnknownError)
      {
         switch (error)
         {
            case UpdateContactInfoError.UnknownError:
            case UpdateContactInfoError.UserNotFound:
               GenericError = "An error occurred";
               break;
            case UpdateContactInfoError.EmailAddressUnavailable:
               EmailAddressError = "Email address unavailable";
               break;
            case UpdateContactInfoError.InvalidEmailAddress:
               EmailAddressError = "Invalid email address";
               break;
            case UpdateContactInfoError.InvalidPassword:
               PasswordError = "Incorrect password";
               break;
            case UpdateContactInfoError.PasswordHashFailure:
               PasswordError = "A cryptographic error occured. This device or browser may not be supported.";
               break;
         }
      }
   }
}
