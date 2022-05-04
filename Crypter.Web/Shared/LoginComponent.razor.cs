﻿/*
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
using Crypter.Web.Helpers;
using Crypter.Web.Models.Forms;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class LoginComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IUserSessionService UserSessionService { get; set; }

      private const string _invalidClassName = "is-invalid";

      protected LoginForm LoginModel;

      protected bool LoginAttemptFailed;
      protected string LoginAttemptErrorMessage;

      protected string UsernameInvalidClassPlaceholder;
      protected string UsernameValidationErrorMessage;

      protected string PasswordInvalidClassPlaceholder;
      protected string PasswordValidationErrorMessage;

      protected override void OnInitialized()
      {
         if (UserSessionService.LoggedIn)
         {
            NavigationManager.NavigateTo("/user/transfers");
            return;
         }

         LoginModel = new()
         {
            RememberMe = true
         };
      }

      protected async Task SubmitLoginAsync()
      {
         var maybeUsername = ValidateUsername();
         var maybePassword = ValidatePassword();

         if (maybeUsername.IsLeft || maybePassword.IsLeft)
         {
            return;
         }

         var username = maybeUsername.RightUnsafe;
         var password = maybePassword.RightUnsafe;

         var authSuccess = await UserSessionService.LoginAsync(username, password, LoginModel.RememberMe);
         if (!authSuccess)
         {
            HandleLoginFailure();
            return;
         }

         var returnUrl = NavigationManager.QueryString("returnUrl") ?? "user/transfers";
         NavigationManager.NavigateTo(returnUrl);
      }

      private Either<Nothing, Username> ValidateUsername()
      {
         var validationResult = Username.CheckValidation(LoginModel.Username);

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
                  or StringPrimitiveValidationFailure.IsEmpty => "Please enter your username",
               _ => "Invalid username"
            };
         });

         return validationResult.Match<Either<Nothing, Username>>(
            () => Username.From(LoginModel.Username),
            _ => new Nothing());
      }

      private Either<Nothing, Password> ValidatePassword()
      {
         var validationResult = Password.CheckValidation(LoginModel.Password);

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
                  or StringPrimitiveValidationFailure.IsEmpty => "Please enter your password",
               _ => "Invalid password"
            };
         });

         return validationResult.Match<Either<Nothing, Password>>(
            () => Password.From(LoginModel.Password),
            _ => new Nothing());
      }

      private void HandleLoginFailure()
      {
         LoginAttemptFailed = true;
         LoginAttemptErrorMessage = "Incorrect username or password";
      }
   }
}
