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
using Crypter.Web.Shared.Modal.Template;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Crypter.Web.Shared.Modal;

public partial class RecoveryKeyModal
{
    [Inject] private IJSRuntime JsRuntime { get; set; }

    [Inject] private ICrypterApiClient CrypterApiService { get; set; }

    [Inject] private IUserKeysService UserKeysService { get; set; }

    [Inject] private IUserRecoveryService UserRecoveryService { get; set; }

    private string _recoveryKey;

    private ModalBehavior _modalBehaviorRef;

    public async Task OpenAsync(Username username, Password password)
    {
        _recoveryKey = await UserKeysService.MasterKey
            .BindAsync(async masterKey =>
                await UserRecoveryService.DeriveRecoveryKeyAsync(masterKey, username, password))
            .MatchAsync(
                () => "An error occurred",
                x => x.ToBase64String());

        _modalBehaviorRef.Open();
    }

    public async Task OpenAsync(Username username, VersionedPassword versionedPassword)
    {
        _recoveryKey = await UserKeysService.MasterKey
            .BindAsync(async masterKey =>
                await UserRecoveryService.DeriveRecoveryKeyAsync(masterKey, versionedPassword))
            .MatchAsync(
                () => "An error occurred",
                x => x.ToBase64String());

        _modalBehaviorRef.Open();
    }

    private async Task CopyRecoveryKeyToClipboardAsync()
    {
        await JsRuntime.InvokeVoidAsync("Crypter.CopyToClipboard",
            new object[] { _recoveryKey, "recoveryKeyModalCopyTooltip" });
    }

    private async Task OnAcknowledgedClickedAsync()
    {
        await CrypterApiService.UserConsent.ConsentToRecoveryKeyRisksAsync();
        _modalBehaviorRef.Close();
    }
}
