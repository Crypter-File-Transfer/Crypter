/*
 * Copyright (C) 2024 Crypter File Transfer
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

using System;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using Crypter.Common.Primitives.Enums;
using Crypter.Web.Helpers;
using Crypter.Web.Models.Forms;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared;

public partial class LoginComponent
{
    [Inject] private NavigationManager NavigationManager { get; init; } = null!;

    [Inject] private IUserSessionService UserSessionService { get; init; } = null!;

    private const string UserLandingPage = "/user/transfers";
    private const string InvalidClassName = "is-invalid";

    private LoginForm _loginModel = new LoginForm
    {
        RememberMe = true
    };

    private bool _loginAttemptFailed;
    private string _loginAttemptErrorMessage = string.Empty;

    private string _usernameInvalidClassPlaceholder = string.Empty;
    private string _usernameValidationErrorMessage = string.Empty;

    private string _passwordInvalidClassPlaceholder = string.Empty;
    private string _passwordValidationErrorMessage = string.Empty;

    private async Task SubmitLoginAsync()
    {
        Task<Either<LoginError, Unit>> loginTask = from username in ValidateUsername().ToEither(LoginError.InvalidUsername).AsTask()
            from password in ValidatePassword().ToEither(LoginError.InvalidPassword).AsTask()
            from loginResult in UserSessionService.LoginAsync(username, password, _loginModel.RememberMe)
            select loginResult;

        Either<LoginError, Unit> loginTaskResult = await loginTask;

        loginTaskResult
            .DoRight(_ =>
            {
                string returnUrl = NavigationManager.GetQueryParameter("returnUrl") ?? UserLandingPage;
                NavigationManager.NavigateTo(returnUrl);
            })
            .DoLeftOrNeither(
                HandleLoginFailure,
                () => HandleLoginFailure(LoginError.UnknownError));
    }

    private Maybe<Username> ValidateUsername()
    {
        return Username.CheckValidation(_loginModel.Username)
            .IfSome(error =>
            {
                _usernameInvalidClassPlaceholder = InvalidClassName;
                _usernameValidationErrorMessage = error switch
                {
                    StringPrimitiveValidationFailure.IsNull
                        or StringPrimitiveValidationFailure.IsEmpty => "Please enter your username",
                    _ => "Invalid username"
                };
            })
            .IfNone(() =>
            {
                _usernameInvalidClassPlaceholder = "";
                _usernameValidationErrorMessage = "";
            })
            .Match(
                () => Username.From(_loginModel.Username),
                _ => Maybe<Username>.None);
    }

    private Maybe<Password> ValidatePassword()
    {
        var validationResult = Password.CheckValidation(_loginModel.Password);

        validationResult.IfNone(() =>
        {
            _passwordInvalidClassPlaceholder = "";
            _passwordValidationErrorMessage = "";
        });

        validationResult.IfSome(error =>
        {
            _passwordInvalidClassPlaceholder = InvalidClassName;
            _passwordValidationErrorMessage = error switch
            {
                StringPrimitiveValidationFailure.IsNull
                    or StringPrimitiveValidationFailure.IsEmpty => "Please enter your password",
                _ => "Invalid password"
            };
        });

        return validationResult.Match(
            () => Password.From(_loginModel.Password),
            _ => Maybe<Password>.None);
    }

    private void HandleLoginFailure(LoginError error)
    {
        _loginAttemptFailed = true;
        _loginAttemptErrorMessage = error switch
        {
            LoginError.UnknownError
                or LoginError.InvalidTokenTypeRequested => "An unknown error occurred",
            LoginError.InvalidUsername
                or LoginError.InvalidPassword => "Invalid username or password",
            LoginError.ExcessiveFailedLoginAttempts => "Too many failed login attempts. Try again later.",
            LoginError.PasswordHashFailure =>
                "A cryptographic error occurred while logging you in. This device or browser may not be supported.",
            LoginError.InvalidPasswordVersion => "Wrong password version",
            _ => throw new ArgumentOutOfRangeException(nameof(error), "Encountered an unknown LoginError")
        };
    }
}
