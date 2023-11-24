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

namespace Crypter.Web.Shared.UserSettings;

public partial class UserSettingsContactInfo
{
    [Inject] private IUserContactInfoSettingsService UserContactInfoSettingsService { get; set; }

    private string _emailAddress = string.Empty;
    private string _emailAddressEdit = string.Empty;

    private bool _emailAddressVerified = false;

    private string _password = string.Empty;

    private bool _isDataReady = false;
    private bool _isEditing = false;

    private string _emailAddressError = string.Empty;
    private string _passwordError = string.Empty;
    private string _genericError = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await UserContactInfoSettingsService.GetContactInfoSettingsAsync()
            .IfSomeAsync(x =>
            {
                _emailAddress = x.EmailAddress;
                _emailAddressEdit = x.EmailAddress;

                _emailAddressVerified = x.EmailAddressVerified;
            });

        _isDataReady = true;
    }

    private void OnEditClicked()
    {
        _isEditing = true;
    }

    private void OnCancelClicked()
    {
        ResetErrors();
        _password = string.Empty;
        _emailAddressEdit = _emailAddress;
        _isEditing = false;
    }

    private void ResetErrors()
    {
        _emailAddressError = string.Empty;
        _passwordError = string.Empty;
        _genericError = string.Empty;
    }

    private async Task OnSaveClickedAsync()
    {
        ResetErrors();

        if (!Password.TryFrom(_password, out var password))
        {
            _passwordError = "Enter your current password";
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
                _emailAddress = x.EmailAddress;
                _emailAddressEdit = x.EmailAddress;

                _emailAddressVerified = x.EmailAddressVerified;
            })
            .DoLeftOrNeitherAsync(
                HandleContactInfoUpdateError,
                () => HandleContactInfoUpdateError());

        _password = string.Empty;
        _isEditing = false;
    }

    private void HandleContactInfoUpdateError(
        UpdateContactInfoSettingsError error = UpdateContactInfoSettingsError.UnknownError)
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
                _passwordError = "Incorrect password";
                break;
            case UpdateContactInfoSettingsError.PasswordHashFailure:
                _passwordError = "A cryptographic error occured. This device or browser may not be supported.";
                break;
            case UpdateContactInfoSettingsError.PasswordNeedsMigration:
                _passwordError =
                    "For security purposes, you must log out then log back in to proceed with this change.";
                break;
            case UpdateContactInfoSettingsError.UnknownError:
            case UpdateContactInfoSettingsError.UserNotFound:
            case UpdateContactInfoSettingsError.InvalidUsername:
            default:
                _genericError = "An error occurred";
                break;
        }
    }
}
