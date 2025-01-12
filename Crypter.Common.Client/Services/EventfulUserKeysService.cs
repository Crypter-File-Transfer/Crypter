/*
 * Copyright (C) 2025 Crypter File Transfer
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
using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Models;
using Crypter.Crypto.Common;
using EasyMonads;

namespace Crypter.Common.Client.Services;

public sealed class EventfulUserKeysService : UserKeysService, IEventfulUserKeysService, IDisposable
{
    private readonly IUserSessionService _userSessionService;
    private EventHandler<EmitRecoveryKeyEventArgs>? _emitRecoveryKeyEventHandler;
    private event EventHandler? _prepareUserKeysBeginEventHandler;
    private event EventHandler? _prepareUserKeysEndEventHandler;
    
    public EventfulUserKeysService(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserPasswordService userPasswordService, IUserKeysRepository userKeysRepository, IUserSessionService userSessionService)
        : base(crypterApiClient, cryptoProvider, userPasswordService, userKeysRepository)
    {
        _userSessionService = userSessionService;
        
        _userSessionService.ServiceInitializedEventHandler += InitializeAsync;
        _userSessionService.UserLoggedInEventHandler += HandleUserLoginAsync;
        _userSessionService.UserLoggedOutEventHandler += Recycle;
    }
    
    private async void InitializeAsync(object? _, UserSessionServiceInitializedEventArgs args)
    {
        if (args.IsLoggedIn)
        {
            MasterKey = await UserKeysRepository.GetMasterKeyAsync();
            PrivateKey = await UserKeysRepository.GetPrivateKeyAsync();
        }
    }
    
    private async void HandleUserLoginAsync(object? _, UserLoggedInEventArgs args)
    {
        await UserPasswordService.DeriveUserCredentialKeyAsync(args.Username, args.Password, UserPasswordService.CurrentPasswordVersion)
            .IfSomeAsync(_ => HandlePrepareUserKeysBeginEvent())
            .BindAsync(async credentialKey => await GetOrCreateMasterKeyAsync(args.VersionedPassword, credentialKey)
                .BindAsync(x => new { CredentialKey = credentialKey, x.MasterKey, x.NewRecoveryKey }))
            .BindAsync(async carryData => await GetOrCreateKeyPairAsync(carryData.MasterKey)
                .BindAsync(x => new { carryData.CredentialKey, carryData.MasterKey, x.PrivateKey, carryData.NewRecoveryKey }))
            .IfSomeAsync(async carryData =>
            {
                await StoreSecretKeysAsync(carryData.MasterKey, carryData.PrivateKey, args.RememberUser);
                HandlePrepareUserKeysEndEvent();
                await carryData.NewRecoveryKey
                    .IfSome(HandleEmitRecoveryKeyEvent)
                    .IfNoneAsync(async () =>
                    {
                        if (args.ShowRecoveryKeyModal)
                        {
                            await DeriveRecoveryKeyAsync(carryData.MasterKey, args.VersionedPassword)
                                .IfSomeAsync(HandleEmitRecoveryKeyEvent);
                        }
                    });
            });
    }
    
    private void HandleEmitRecoveryKeyEvent(RecoveryKey recoveryKey) =>
        _emitRecoveryKeyEventHandler?.Invoke(this, new EmitRecoveryKeyEventArgs(recoveryKey));
    
    private void HandlePrepareUserKeysBeginEvent() =>
        _prepareUserKeysBeginEventHandler?.Invoke(this, EventArgs.Empty);
    
    private void HandlePrepareUserKeysEndEvent() =>
        _prepareUserKeysEndEventHandler?.Invoke(this, EventArgs.Empty);
    
    public event EventHandler<EmitRecoveryKeyEventArgs> EmitRecoveryKeyEventHandler
    {
        add => _emitRecoveryKeyEventHandler = 
            (EventHandler<EmitRecoveryKeyEventArgs>)Delegate.Combine(_emitRecoveryKeyEventHandler, value);
        remove => _emitRecoveryKeyEventHandler =
            (EventHandler<EmitRecoveryKeyEventArgs>?)Delegate.Remove(_emitRecoveryKeyEventHandler, value);
    }
    
    public event EventHandler PrepareUserKeysBeginEventHandler
    {
        add => _prepareUserKeysBeginEventHandler = 
            (EventHandler)Delegate.Combine(_prepareUserKeysBeginEventHandler, value);
        remove => _prepareUserKeysBeginEventHandler =
            (EventHandler?)Delegate.Remove(_prepareUserKeysBeginEventHandler, value);
    }
    
    public event EventHandler PrepareUserKeysEndEventHandler
    {
        add => _prepareUserKeysEndEventHandler = 
            (EventHandler)Delegate.Combine(_prepareUserKeysEndEventHandler, value);
        remove => _prepareUserKeysEndEventHandler =
            (EventHandler?)Delegate.Remove(_prepareUserKeysEndEventHandler, value);
    }
    
    private void Recycle(object? _, EventArgs __)
    {
        MasterKey = Maybe<byte[]>.None;
        PrivateKey = Maybe<byte[]>.None;
    }

    public void Dispose()
    {
        _userSessionService.ServiceInitializedEventHandler -= InitializeAsync;
        _userSessionService.UserLoggedInEventHandler -= HandleUserLoginAsync;
        _userSessionService.UserLoggedOutEventHandler -= Recycle;
    }
}
