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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserAuthentication.PasswordChange;
using Crypter.Common.Contracts.Features.UserSettings.ContactInfoSettings;
using Crypter.Common.Primitives;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.UserSettings;

public partial class UserSettingsAccountInfo
{
    [Inject] private IUserContactInfoSettingsService UserContactInfoSettingsService { get; init; } = null!;

    [Inject] private IUserPasswordChangeService UserPasswordChangeService { get; init; } = null!;
    
    private string _emailAddress = string.Empty;
    private string _emailAddressEdit = string.Empty;

    private bool _emailAddressVerified = false;

    private string _emailAddressPassword = string.Empty;
    private string _passwordChangeOldPassword = string.Empty;
    private string _passwordChangeNewPassword = string.Empty;
    private string _passwordChangeConfirmPassword = string.Empty;

    private bool _isDataReady = false;
    private bool _isEditingEmailAddress = false;
    private bool _isEditingPassword = false;

    private string _emailAddressError = string.Empty;
    private string _emailAddressPasswordError = string.Empty;
    private string _genericEmailAddressError = string.Empty;

    private string _oldPasswordError = string.Empty;
    private string _newPasswordError = string.Empty;
    private string _confirmPasswordError = string.Empty;
    private string _passwordChangeError = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await UserContactInfoSettingsService.GetContactInfoSettingsAsync()
            .IfSomeAsync(x =>
            {
                _emailAddress = x.EmailAddress ?? string.Empty;
                _emailAddressEdit = x.EmailAddress ?? string.Empty;

                _emailAddressVerified = x.EmailAddressVerified;
            });

        _isDataReady = true;
    }

    private void OnEditContactInfoClicked()
    {
        _isEditingEmailAddress = true;
    }

    private void OnChangePasswordClicked()
    {
        _isEditingPassword = true;
    }
    
    private void OnCancelForEditContactInfoClicked()
    {
        ResetContactInfoErrors();
        _emailAddressPassword = string.Empty;
        _emailAddressEdit = _emailAddress;
        _isEditingEmailAddress = false;
    }

    private void OnCancelForChangePasswordClicked()
    {
        ResetPasswordChangeErrors();
        _passwordChangeOldPassword = string.Empty;
        _passwordChangeNewPassword = string.Empty;
        _passwordChangeConfirmPassword = string.Empty;
        _isEditingPassword = false;

    }
    
    private void ResetContactInfoErrors()
    {
        _emailAddressError = string.Empty;
        _emailAddressPasswordError = string.Empty;
        _genericEmailAddressError = string.Empty;
        _passwordChangeError = string.Empty;
    }

    private void ResetPasswordChangeErrors()
    {
        _oldPasswordError = string.Empty;
        _newPasswordError = string.Empty;
        _confirmPasswordError = string.Empty;
        _passwordChangeError = string.Empty;
    }
    
    private async Task OnSaveContactInfoClickedAsync()
    {
        ResetContactInfoErrors();

        if (!Password.TryFrom(_emailAddressPassword, out Password password))
        {
            _emailAddressPasswordError = "Enter your current password";
            return;
        }

        bool someEmailAddress = !string.IsNullOrEmpty(_emailAddressEdit);
        bool validEmailAddress = EmailAddress.TryFrom(_emailAddressEdit, out EmailAddress emailAddress);

        if (someEmailAddress && !validEmailAddress)
        {
            _emailAddressError = "You must either enter a valid email address or provide a blank value";
            return;
        }

        await UserContactInfoSettingsService.UpdateContactInfoSettingsAsync(emailAddress, password)
            .DoRightAsync(x =>
            {
                _emailAddress = x.EmailAddress ?? string.Empty;
                _emailAddressEdit = x.EmailAddress ?? string.Empty;

                _emailAddressVerified = x.EmailAddressVerified;
            })
            .DoLeftOrNeitherAsync(
                HandleContactInfoUpdateError,
                () => HandleContactInfoUpdateError());

        _emailAddressPassword = string.Empty;
        _isEditingEmailAddress = false;
    }

    private async Task OnSavePasswordChangeClickAsync()
    {
        ResetPasswordChangeErrors();

        if (!Password.TryFrom(_passwordChangeOldPassword, out Password oldPassword))
        {
            _oldPasswordError = "Enter your current password";
            return;
        }

        if (!Password.TryFrom(_passwordChangeNewPassword, out Password newPassword))
        {
            _newPasswordError = "Enter your new password";
            return;
        }

        if (!Password.TryFrom(_passwordChangeConfirmPassword, out Password confirmPassword) || newPassword.Value != confirmPassword.Value)
        {
            _confirmPasswordError = "Passwords do not match";
            return;
        }

        await UserPasswordChangeService.ChangePasswordAsync(oldPassword, newPassword)
            .DoRightAsync(_ => OnCancelForChangePasswordClicked())
            .DoLeftOrNeitherAsync(
                HandlePasswordChangeError,
                () => HandlePasswordChangeError());
    }

    private void HandleContactInfoUpdateError(UpdateContactInfoSettingsError error = UpdateContactInfoSettingsError.UnknownError)
    {
        switch (error)
        {
            case UpdateContactInfoSettingsError.EmailAddressUnavailable:
                _emailAddressError = "Email address unavailable";
                break;
            case UpdateContactInfoSettingsError.InvalidEmailAddress:
                _emailAddressError = "Invalid email address";
                break;
            case UpdateContactInfoSettingsError.InvalidPassword:
                _emailAddressPasswordError = "Incorrect password";
                break;
            case UpdateContactInfoSettingsError.PasswordHashFailure:
                _emailAddressPasswordError = "A cryptographic error occured. This device or browser may not be supported.";
                break;
            case UpdateContactInfoSettingsError.PasswordNeedsMigration:
                _emailAddressPasswordError = "For security purposes, you must log out then log back in to proceed with this change.";
                break;
            case UpdateContactInfoSettingsError.UnknownError:
            case UpdateContactInfoSettingsError.UserNotFound:
            case UpdateContactInfoSettingsError.InvalidUsername:
            default:
                _genericEmailAddressError = "An error occurred";
                break;
        }
    }

    private void HandlePasswordChangeError(PasswordChangeError error = PasswordChangeError.UnknownError)
    {
        switch (error)
        {
            case PasswordChangeError.InvalidPassword:
                _oldPasswordError = "Incorrect password";
                break;
            case PasswordChangeError.PasswordHashFailure:
                _passwordChangeError = "A cryptographic error occured. This device or browser may not be supported.";
                break;
            case PasswordChangeError.InvalidOldPasswordVersion:
            case PasswordChangeError.InvalidNewPasswordVersion:
            case PasswordChangeError.UnknownError:
            default:
                _passwordChangeError = "An error occured";
                break;
        }
    }
}
