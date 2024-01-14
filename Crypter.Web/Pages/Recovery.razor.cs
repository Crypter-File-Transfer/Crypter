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

using System.Collections.Specialized;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.AccountRecovery.SubmitRecovery;
using Crypter.Common.Infrastructure;
using Crypter.Common.Primitives;
using Crypter.Web.Helpers;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Pages;

public partial class Recovery
{
    [Inject] private NavigationManager NavigationManager { get; set; }

    [Inject] private IUserRecoveryService UserRecoveryService { get; set; }

    private string _recoveryCode;

    private string _recoverySignature;

    private bool _recoveryKeySwitch = true;

    private Username _username;
    private string _newPassword;
    private string _newPasswordConfirm;
    private string _recoveryKeyInput;

    private bool _recoverySucceeded;

    private string _recoveryErrorMessage;

    protected override void OnInitialized()
    {
        NameValueCollection queryParameters = NavigationManager.GetQueryParameters();

        bool validPageLanding = !string.IsNullOrEmpty(queryParameters["username"])
                                && !string.IsNullOrEmpty(queryParameters["code"])
                                && !string.IsNullOrEmpty(queryParameters["signature"]);

        if (!validPageLanding)
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        string decodedUsername = UrlSafeEncoder.DecodeStringUrlSafe(queryParameters["username"]);
        if (Username.TryFrom(decodedUsername, out Username validUsername))
        {
            _username = validUsername;
        }
        else
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        _recoveryCode = queryParameters["code"];
        _recoverySignature = queryParameters["signature"];
    }

    private async Task SubmitRecoveryAsync()
    {
        if (_newPassword != _newPasswordConfirm)
        {
            _recoveryErrorMessage = "Passwords do not match.";
            return;
        }

        if (!Password.TryFrom(_newPassword, out Password validPassword))
        {
            _recoveryErrorMessage = "Invalid password.";
            return;
        }

        Either<SubmitAccountRecoveryError, Maybe<RecoveryKey>> recoveryResult = _recoveryKeySwitch
            ? await RecoveryKey.FromBase64String(_recoveryKeyInput)
                .ToEither(SubmitAccountRecoveryError.WrongRecoveryKey)
                .BindAsync(async x =>
                    await UserRecoveryService.SubmitRecoveryRequestAsync(_recoveryCode, _recoverySignature, _username,
                        validPassword, x))
            : await UserRecoveryService.SubmitRecoveryRequestAsync(_recoveryCode, _recoverySignature, _username,
                validPassword, Maybe<RecoveryKey>.None);

#pragma warning disable CS8524
        recoveryResult.DoLeftOrNeither(
            errorCode => _recoveryErrorMessage = errorCode switch
            {
                SubmitAccountRecoveryError.UnknownError => "An unknown error occurred.",
                SubmitAccountRecoveryError.InvalidUsername => "Invalid username.",
                SubmitAccountRecoveryError.RecoveryNotFound =>
                    "This recovery link is expired. Request a new recovery link and try again.",
                SubmitAccountRecoveryError.WrongRecoveryKey => "The recovery key you provided is invalid.",
                SubmitAccountRecoveryError.InvalidMasterKey => "Invalid master key information.",
                SubmitAccountRecoveryError.PasswordHashFailure =>
                    "A cryptographic error occurred while securing your new password. This device or browser may not be supported."
            },
            () => _recoveryErrorMessage = "An unknown error occurred.");
#pragma warning restore CS8524

        _recoverySucceeded = recoveryResult.IsRight;
    }
}
