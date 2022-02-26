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
using Crypter.Web.Helpers;
using Crypter.Web.Models.Forms;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class LoginComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ISessionService SessionService { get; set; }

      [Inject]
      IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation> BrowserStorageService { get; set; }

      protected LoginModel LoginModel = new();

      protected bool LoginError = false;
      protected string LoginErrorText = "";

      protected string IsInvalid = "is-invalid";

      protected string UsernameInvalidClass = "";
      protected string UsernameValidationMessage;
      private readonly static string MissingUsername = "Please enter your username";

      protected string PasswordInvalidClass = "";
      protected string PasswordValidationMessage;
      private readonly static string MissingPassword = "Please enter your password";

      protected override async Task OnInitializedAsync()
      {
         if (BrowserStorageService.HasItem(BrowserStoredObjectType.UserSession))
         {
            NavigationManager.NavigateTo("/user/transfers");
         }
         await base.OnInitializedAsync();
      }

      protected async Task OnLoginClickedAsync()
      {
         if (!ValidateForm())
         {
            return;
         }

         var authSuccess = await SessionService.LoginAsync(LoginModel.Username, LoginModel.Password, LoginModel.RememberMe);
         if (authSuccess)
         {
            var returnUrl = NavigationManager.QueryString("returnUrl") ?? "user/transfers";
            NavigationManager.NavigateTo(returnUrl);
            return;
         }

         LoginError = true;
         LoginErrorText = "Incorrect username or password";
      }

      private bool ValidateForm()
      {
         return ValidateUsername()
            && ValidatePassword();
      }

      private bool ValidateUsername()
      {
         if (string.IsNullOrEmpty(LoginModel.Username))
         {
            UsernameValidationMessage = MissingUsername;
            UsernameInvalidClass = IsInvalid;
            return false;
         }

         UsernameInvalidClass = "";
         return true;
      }

      private bool ValidatePassword()
      {
         if (string.IsNullOrEmpty(LoginModel.Password))
         {
            PasswordValidationMessage = MissingPassword;
            PasswordInvalidClass = IsInvalid;
            return false;
         }

         PasswordInvalidClass = "";
         return true;
      }
   }
}
