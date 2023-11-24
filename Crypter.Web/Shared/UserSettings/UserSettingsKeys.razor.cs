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

using System;
using System.Threading.Tasks;
using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Web.Shared.Modal;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Crypter.Web.Shared.UserSettings;

public partial class UserSettingsKeys : IDisposable
{
    [Inject] private IUserSessionService UserSessionService { get; set; }

    [Inject] private IUserKeysService UserKeysService { get; set; }

    [Inject] private IUserRecoveryService UserRecoveryService { get; set; }

    [Inject] private IJSRuntime JsRuntime { get; set; }

    private PasswordModal _passwordModal;

    private string _privateKey = string.Empty;
    private string _recoveryKey = string.Empty;

    protected override void OnInitialized()
    {
        _privateKey = UserKeysService.PrivateKey.Match(
            () => string.Empty,
            Convert.ToHexString);

        UserSessionService.UserPasswordTestSuccessEventHandler += OnPasswordTestSuccess;
    }

    private async void OnPasswordTestSuccess(object sender, UserPasswordTestSuccessEventArgs args)
    {
        _recoveryKey = await UserKeysService.MasterKey
            .BindAsync(async masterKey =>
                await UserRecoveryService.DeriveRecoveryKeyAsync(masterKey, args.Username, args.Password))
            .MatchAsync(
                () => "An error occurred",
                x => x.ToBase64String());

        await InvokeAsync(StateHasChanged);
    }

    private async Task CopyRecoveryKeyToClipboardAsync()
    {
        await JsRuntime.InvokeVoidAsync("Crypter.CopyToClipboard",
            new object[] { _recoveryKey, "recoveryKeyCopyTooltip" });
    }

    public void Dispose()
    {
        UserSessionService.UserPasswordTestSuccessEventHandler -= OnPasswordTestSuccess;
        GC.SuppressFinalize(this);
    }
}
