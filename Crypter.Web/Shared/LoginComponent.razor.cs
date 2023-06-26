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
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using Crypter.Common.Primitives.Enums;
using Crypter.Web.Helpers;
using Crypter.Web.Models.Forms;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared
{
   public partial class LoginComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IUserSessionService UserSessionService { get; set; }

      private const string _userLandingPage = "/user/transfers";
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
         LoginModel = new LoginForm()
         {
            RememberMe = true
         };
      }

      protected async Task SubmitLoginAsync()
      {
         var loginTask = from username in ValidateUsername().ToEither(LoginError.InvalidUsername).AsTask()
                         from password in ValidatePassword().ToEither(LoginError.InvalidPassword).AsTask()
                         from loginResult in UserSessionService.LoginAsync(username, password, LoginModel.RememberMe)
                         select loginResult;

         var loginTaskResult = await loginTask;

         loginTaskResult.DoLeftOrNeither(
            left => HandleLoginFailure(left),
            () => HandleLoginFailure(LoginError.UnknownError));


         loginTaskResult.DoRight(_ =>
         {
            string returnUrl = NavigationManager.GetQueryParameter("returnUrl") ?? _userLandingPage;
            NavigationManager.NavigateTo(returnUrl);
         });
      }

      private Maybe<Username> ValidateUsername()
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

         return validationResult.Match(
            () => Username.From(LoginModel.Username),
            _ => Maybe<Username>.None);
      }

      private Maybe<Password> ValidatePassword()
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

         return validationResult.Match(
           () => Password.From(LoginModel.Password),
           _ => Maybe<Password>.None);
      }

      private void HandleLoginFailure(LoginError error)
      {
         LoginAttemptFailed = true;
#pragma warning disable CS8524
         LoginAttemptErrorMessage = error switch
         {
            LoginError.UnknownError
               or LoginError.InvalidTokenTypeRequested => "An unknown error occurred",
            LoginError.InvalidUsername
               or LoginError.InvalidPassword => "Invalid username or password",
            LoginError.ExcessiveFailedLoginAttempts => "Too many failed login attempts. Try again later.",
            LoginError.PasswordHashFailure => "A cryptographic error occurred while logging you in. This device or browser may not be supported.",
            LoginError.InvalidPasswordVersion => "Wrong password version"
         };
#pragma warning restore CS8524
      }
   }
}
