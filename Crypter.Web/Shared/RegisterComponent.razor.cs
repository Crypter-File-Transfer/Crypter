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
    [Inject] private ICrypterApiClient CrypterApiService { get; set; }

    [Inject] private IUserSessionService UserSessionService { get; set; }

    [Inject] private IUserPasswordService UserPasswordService { get; set; }

    private const string InvalidClassName = "is-invalid";

    private UserRegistrationForm _registrationModel;

    private bool _registrationAttemptFailed;
    private bool _registrationAttemptSucceeded;
    private string _registrationAttemptErrorMessage;

    private string _usernameInvalidClassPlaceholder;
    private string _usernameValidationErrorMessage;

    private string _passwordInvalidClassPlaceholder;
    private string _passwordValidationErrorMessage;

    private string _passwordConfirmInvalidClassPlaceholder;
    private string _passwordConfirmValidationErrorMessage;

    private string _emailAddressInvalidClassPlaceholder;
    private string _emailAddressValidationErrorMessage;

    private bool _userProvidedEmailAddress;

    protected override void OnInitialized()
    {
        _registrationModel = new UserRegistrationForm();
    }

    private Maybe<Username> ValidateUsername()
    {
        Maybe<StringPrimitiveValidationFailure> invalidReason = Username.CheckValidation(_registrationModel.Username);

        invalidReason.IfNone(() =>
        {
            _usernameInvalidClassPlaceholder = string.Empty;
            _usernameValidationErrorMessage = string.Empty;
        });

        invalidReason.IfSome(error =>
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
        });

        return invalidReason.Match(
            () => Username.From(_registrationModel.Username),
            _ => Maybe<Username>.None);
    }

    private Maybe<Password> ValidatePassword()
    {
        Maybe<StringPrimitiveValidationFailure> invalidReason = Password.CheckValidation(_registrationModel.Password);

        invalidReason.IfNone(() =>
        {
            _passwordInvalidClassPlaceholder = string.Empty;
            _passwordValidationErrorMessage = string.Empty;
        });

        invalidReason.IfSome(error =>
        {
            _passwordInvalidClassPlaceholder = InvalidClassName;
            _passwordValidationErrorMessage = error switch
            {
                StringPrimitiveValidationFailure.IsNull
                    or StringPrimitiveValidationFailure.IsEmpty => "Please enter a password",
                _ => "Invalid password"
            };
        });

        return invalidReason.Match(
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

        Maybe<StringPrimitiveValidationFailure> invalidReason = EmailAddress.CheckValidation(_registrationModel.EmailAddress);

        invalidReason.IfNone(() =>
        {
            _emailAddressInvalidClassPlaceholder = string.Empty;
            _emailAddressValidationErrorMessage = string.Empty;
        });

        invalidReason.IfSome(error =>
        {
            _emailAddressInvalidClassPlaceholder = InvalidClassName;
            _emailAddressValidationErrorMessage = "Invalid email address";
        });

        return invalidReason.Match<Either<Unit, Maybe<EmailAddress>>>(
            () => Maybe<EmailAddress>.From(EmailAddress.From(_registrationModel.EmailAddress)),
            _ => Unit.Default);
    }

    protected async Task SubmitRegistrationAsync()
    {
        var registrationResult = await ValidateUsername()
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
                    })));

        registrationResult.IfSome(x => x.DoLeftOrNeither(HandleRegistrationFailure, HandleUnknownRegistrationFailure));
        _registrationAttemptSucceeded = registrationResult.Match(
            () => false,
            x => x.IsRight
        );
    }

    private void HandleRegistrationFailure(RegistrationError error)
    {
        _registrationAttemptFailed = error != RegistrationError.InvalidPasswordConfirm;
#pragma warning disable CS8524
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
            RegistrationError.InvalidPasswordConfirm => string.Empty
        };
#pragma warning restore CS8524
    }

    private void HandleUnknownRegistrationFailure()
    {
        _registrationAttemptFailed = true;
        _registrationAttemptErrorMessage = "An unknown error occurred.";
    }
}
