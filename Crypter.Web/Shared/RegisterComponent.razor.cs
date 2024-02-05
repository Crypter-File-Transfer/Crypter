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
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using Crypter.Common.Primitives.Enums;
using Crypter.Web.Models.Forms;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared;

public partial class RegisterComponent
{
    [Inject] private ICrypterApiClient CrypterApiService { get; init; } = null!;

    [Inject] private IUserSessionService UserSessionService { get; init; } = null!;

    [Inject] private IUserPasswordService UserPasswordService { get; init; } = null!;

    private const string InvalidClassName = "is-invalid";

    private UserRegistrationForm _registrationModel = new UserRegistrationForm();

    private bool _registrationAttemptFailed;
    private bool _registrationAttemptSucceeded;
    private string _registrationAttemptErrorMessage = string.Empty;

    private string _usernameInvalidClassPlaceholder = string.Empty;
    private string _usernameValidationErrorMessage = string.Empty;

    private string _passwordInvalidClassPlaceholder = string.Empty;
    private string _passwordValidationErrorMessage = string.Empty;

    private string _passwordConfirmInvalidClassPlaceholder = string.Empty;
    private string _passwordConfirmValidationErrorMessage = string.Empty;

    private string _emailAddressInvalidClassPlaceholder = string.Empty;
    private string _emailAddressValidationErrorMessage = string.Empty;

    private bool _userProvidedEmailAddress;
    
    private Maybe<Username> ValidateUsername()
    {
        return Username.CheckValidation(_registrationModel.Username)
            .IfSome(error =>
            {
                _usernameInvalidClassPlaceholder = InvalidClassName;
                _usernameValidationErrorMessage = error switch
                {
                    StringPrimitiveValidationFailure.IsNull
                        or StringPrimitiveValidationFailure.IsEmpty => "Please choose a username",
                    StringPrimitiveValidationFailure.TooLong => "Username exceeds 32-character limit",
                    StringPrimitiveValidationFailure.InvalidCharacters => "Username contains invalid character(s)",
                    _ => "Invalid username"
                };
            })
            .IfNone(() =>
            {
                _usernameInvalidClassPlaceholder = string.Empty;
                _usernameValidationErrorMessage = string.Empty;
            })
            .Match(
                () => Username.From(_registrationModel.Username),
                _ => Maybe<Username>.None);
    }

    private Maybe<Password> ValidatePassword()
    {
        return Password.CheckValidation(_registrationModel.Password)
            .IfSome(error =>
            {
                _passwordInvalidClassPlaceholder = InvalidClassName;
                _passwordValidationErrorMessage = error switch
                {
                    StringPrimitiveValidationFailure.IsNull
                        or StringPrimitiveValidationFailure.IsEmpty => "Please enter a password",
                    _ => "Invalid password"
                };
            })
            .IfNone(() =>
            {
                _passwordInvalidClassPlaceholder = string.Empty;
                _passwordValidationErrorMessage = string.Empty;
            })
            .Match(
                () => Password.From(_registrationModel.Password),
                _ => Maybe<Password>.None);
    }

    private bool ValidatePasswordConfirmation()
    {
        bool passwordConfirmMissing = string.IsNullOrEmpty(_registrationModel.PasswordConfirm);
        if (passwordConfirmMissing)
        {
            _passwordConfirmInvalidClassPlaceholder = InvalidClassName;
            _passwordConfirmValidationErrorMessage = "Please confirm your password";
            return false;
        }

        bool passwordsMatch = _registrationModel.Password == _registrationModel.PasswordConfirm;
        if (!passwordsMatch)
        {
            _passwordConfirmInvalidClassPlaceholder = InvalidClassName;
            _passwordConfirmValidationErrorMessage = "Passwords do not match";
            return false;
        }

        _passwordConfirmInvalidClassPlaceholder = string.Empty;
        _passwordConfirmValidationErrorMessage = string.Empty;
        return true;
    }

    private Either<Unit, Maybe<EmailAddress>> ValidateEmailAddress()
    {
        bool isEmailAddressEmpty = string.IsNullOrEmpty(_registrationModel.EmailAddress);
        if (isEmailAddressEmpty)
        {
            return Maybe<EmailAddress>.None;
        }

        return EmailAddress.CheckValidation(_registrationModel.EmailAddress)
            .IfSome(_ =>
            {
                _emailAddressInvalidClassPlaceholder = InvalidClassName;
                _emailAddressValidationErrorMessage = "Invalid email address";
            })
            .IfNone(() =>
            {
                _emailAddressInvalidClassPlaceholder = string.Empty;
                _emailAddressValidationErrorMessage = string.Empty;
            })
            .Match<Either<Unit, Maybe<EmailAddress>>>(
                () => Maybe<EmailAddress>.From(EmailAddress.From(_registrationModel.EmailAddress)),
                _ => Unit.Default);
    }

    private async Task SubmitRegistrationAsync()
    {
        _registrationAttemptSucceeded = await ValidateUsername()
            .BindAsync(username => ValidatePassword()
                .MapAsync(password => ValidateEmailAddress().MapLeft(_ => RegistrationError.InvalidEmailAddress)
                    .BindAsync(async emailAddress =>
                    {
                        _userProvidedEmailAddress = emailAddress.IsSome;

                        if (!ValidatePasswordConfirmation())
                        {
                            return RegistrationError.InvalidPasswordConfirm;
                        }

                        return await UserPasswordService.DeriveUserAuthenticationPasswordAsync(username, password,
                                UserPasswordService.CurrentPasswordVersion)
                            .ToEitherAsync(RegistrationError.PasswordHashFailure)
                            .BindAsync(async versionedPassword =>
                            {
                                RegistrationRequest request =
                                    new RegistrationRequest(username, versionedPassword, emailAddress);
                                return await CrypterApiService.UserAuthentication.RegisterAsync(request);
                            });
                    })))
            .IfSomeAsync(x => x.DoLeftOrNeither(HandleRegistrationFailure, HandleUnknownRegistrationFailure))
            .MatchAsync(
                () => false,
                x => x.IsRight);
    }

    private void HandleRegistrationFailure(RegistrationError error)
    {
        _registrationAttemptFailed = error != RegistrationError.InvalidPasswordConfirm;
        
        _registrationAttemptErrorMessage = error switch
        {
            RegistrationError.InvalidUsername => "Invalid username",
            RegistrationError.InvalidPassword => "Invalid password",
            RegistrationError.InvalidEmailAddress => "Invalid email address",
            RegistrationError.UsernameTaken => "Username is already taken",
            RegistrationError.EmailAddressTaken => "Email address is associated with an existing account",
            RegistrationError.PasswordHashFailure =>
                "A cryptographic error occurred. This device or browser may not be supported.",
            RegistrationError.OldPasswordVersion
                or RegistrationError.UnknownError => "An unknown error occurred.",
            RegistrationError.InvalidPasswordConfirm => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(error), "Encountered an unknown RegistrationError")
        };
    }

    private void HandleUnknownRegistrationFailure()
    {
        _registrationAttemptFailed = true;
        _registrationAttemptErrorMessage = "An unknown error occurred.";
    }
}
