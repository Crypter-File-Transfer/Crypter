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
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Web.Shared.Modal.Template;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Crypter.Web.Shared.Modal;

public partial class RecoveryKeyModal
{
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject] private ICrypterApiClient CrypterApiService { get; set; } = null!;

    [Inject] private IUserKeysService UserKeysService { get; set; } = null!;

    [Inject] private IUserRecoveryService UserRecoveryService { get; set; } = null!;

    private string _recoveryKey = string.Empty;

    private ModalBehavior _modalBehaviorRef = null!;
    
    private bool _isOpen = false;

    public async Task OpenAsync(VersionedPassword versionedPassword)
    {
        if (!_isOpen)
        {
            _isOpen = true;
            Maybe<RecoveryKey> recoveryKey = await UserKeysService.MasterKey
                .BindAsync(async masterKey => await UserRecoveryService.DeriveRecoveryKeyAsync(masterKey, versionedPassword));
            _recoveryKey = RecoveryKeyToString(recoveryKey);
            _modalBehaviorRef.Open();
        }
    }

    public void Open(Maybe<RecoveryKey> recoveryKey)
    {
        if (!_isOpen)
        {
            _isOpen = true;
            _recoveryKey = RecoveryKeyToString(recoveryKey);
            _modalBehaviorRef.Open();
        }
    }

    private string RecoveryKeyToString(Maybe<RecoveryKey> recoveryKey)
    {
        return recoveryKey
            .Match(
                none: () => "An error occurred",
                some: x => x.ToBase64String());
    }

    private async Task CopyRecoveryKeyToClipboardAsync()
    {
        await JsRuntime.InvokeVoidAsync("Crypter.CopyToClipboard", _recoveryKey, "recoveryKeyModalCopyTooltip");
    }

    private async Task OnAcknowledgedClickedAsync()
    {
        await CrypterApiService.UserConsent.ConsentToRecoveryKeyRisksAsync();
        _modalBehaviorRef.Close();
        _isOpen = false;
    }
}
