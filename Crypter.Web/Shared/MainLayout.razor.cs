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
using System.Runtime.Versioning;
using System.Threading.Tasks;
using BlazorSodium.Services;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Web.Services;
using Crypter.Web.Shared.Modal;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared;

[SupportedOSPlatform("browser")]
public class MainLayoutBase : LayoutComponentBase, IDisposable
{
    [Inject] private IBlazorSodiumService BlazorSodiumService { get; init; } = null!;
    
    [Inject] private IEventfulUserKeysService EventfulUserKeysService { get; init; } = null!;

    [Inject] private IUserPasswordService UserPasswordService { get; init; } = null!;

    [Inject] private IDeviceRepository<BrowserStorageLocation> BrowserRepository { get; init; } = null!;

    [Inject] private IFileSaverService FileSaverService { get; init; } = null!;
    
    protected BasicModal BasicModal { get; set; } = null!;
    protected RecoveryKeyModal RecoveryKeyModal { get; set; } = null!;
    protected TransferSuccessModal TransferSuccessModal { get; set; } = null!;
    protected SpinnerModal SpinnerModal { get; set; } = null!;

    protected bool ServicesInitialized { get; set; }

    protected override async Task OnInitializedAsync()
    {
        EventfulUserKeysService.EmitRecoveryKeyEventHandler += HandleRecoveryKeyCreatedEvent;
        UserPasswordService.PasswordHashBeginEventHandler += ShowPasswordHashingModal;
        UserPasswordService.PasswordHashEndEventHandler += ClosePasswordHashingModal;
        await BrowserRepository.InitializeAsync();

        await Task.WhenAll(BlazorSodiumService.InitializeAsync(), FileSaverService.InitializeAsync(), BrowserFunctions.InitializeAsync());

        ServicesInitialized = true;
    }

    private void HandleRecoveryKeyCreatedEvent(object? _, EmitRecoveryKeyEventArgs args)
    {
        RecoveryKeyModal.Open(args.RecoveryKey);
    }
    
    private void ShowPasswordHashingModal(object? _, PasswordHashBeginEventArgs args)
    {
        switch (args.HashType)
        {
            case PasswordHashType.AuthenticationKey:
                SpinnerModal.Open("Securing your Password",
                    "Please wait while your password is securely hashed. Your real password is never provided to the server.",
                    Maybe<EventCallback>.None);
                break;
            case PasswordHashType.CredentialKey:
                SpinnerModal.Open("Calculating Encryption Key",
                    "Please wait while your personal encryption key is calculated.", Maybe<EventCallback>.None);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(args), "Args contained an invalid HashType");
        }
    }

    private async void ClosePasswordHashingModal(object? _, PasswordHashEndEventArgs args)
    {
        await SpinnerModal.CloseAsync();
    }

    public void Dispose()
    {
        EventfulUserKeysService.EmitRecoveryKeyEventHandler -= HandleRecoveryKeyCreatedEvent;
        UserPasswordService.PasswordHashBeginEventHandler -= ShowPasswordHashingModal;
        UserPasswordService.PasswordHashEndEventHandler -= ClosePasswordHashingModal;
        GC.SuppressFinalize(this);
    }
}
