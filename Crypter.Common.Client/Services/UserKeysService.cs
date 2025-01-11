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
using System.Threading.Tasks;
using Crypter.Common.Client.Events;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.KeyExchange;
using EasyMonads;

namespace Crypter.Common.Client.Services;

public class UserKeysService : IUserKeysService, IDisposable
{
    private readonly ICrypterApiClient _crypterApiClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IUserPasswordService _userPasswordService;
    private readonly IUserKeysRepository _userKeysRepository;
    private readonly IUserSessionService _userSessionService;

    private EventHandler<RecoveryKeyCreatedEventArgs>? _recoveryKeyCreatedEventHandler;
    
    public Maybe<byte[]> MasterKey { get; private set; } = Maybe<byte[]>.None;
    public Maybe<byte[]> PrivateKey { get; private set; } = Maybe<byte[]>.None;
    
    public UserKeysService(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserPasswordService userPasswordService, IUserKeysRepository userKeysRepository, IUserSessionService userSessionService)
    {
        _crypterApiClient = crypterApiClient;
        _userPasswordService = userPasswordService;
        _cryptoProvider = cryptoProvider;
        _userKeysRepository = userKeysRepository;
        _userSessionService = userSessionService;

        _userSessionService.ServiceInitializedEventHandler += InitializeAsync;
        _userSessionService.UserLoggedInEventHandler += HandleUserLoginAsync;
        _userSessionService.UserLoggedOutEventHandler += Recycle;
    }
    
    private async void InitializeAsync(object? _, UserSessionServiceInitializedEventArgs args)
    {
        if (args.IsLoggedIn)
        {
            MasterKey = await _userKeysRepository.GetMasterKeyAsync();
            PrivateKey = await _userKeysRepository.GetPrivateKeyAsync();
        }
    }

    private async void HandleUserLoginAsync(object? _, UserLoggedInEventArgs args)
    {
        await _userPasswordService.DeriveUserCredentialKeyAsync(args.Username, args.Password, _userPasswordService.CurrentPasswordVersion)
            .BindAsync(async credentialKey => await GetOrCreateMasterKeyAsync(args.VersionedPassword, credentialKey)
                .BindAsync(x => new { CredentialKey = credentialKey, x.MasterKey, x.RecoveryKey }))
            .BindAsync(async carryData => await GetOrCreateKeyPairAsync(carryData.MasterKey)
                .BindAsync(x => new { carryData.CredentialKey, carryData.MasterKey, x.PrivateKey, carryData.RecoveryKey }))
            .IfSomeAsync(async carryData =>
            {
                await StoreSecretKeysAsync(carryData.MasterKey, carryData.PrivateKey, args.RememberUser);
                carryData.RecoveryKey.IfSome(HandleRecoveryKeyCreatedEvent);
            });
    }

    /// <summary>
    /// Get the existing master key from the API and decrypt it.
    /// If the master key does not already exist, create and upload a new one.
    /// </summary>
    /// <param name="versionedPassword"></param>
    /// <param name="credentialKey"></param>
    /// <returns></returns>
    private async Task<Maybe<GetOrCreateMasterKeyResult>> GetOrCreateMasterKeyAsync(VersionedPassword versionedPassword, byte[] credentialKey)
    {
        return await _crypterApiClient.UserKey.GetMasterKeyAsync()
            .BindAsync<GetMasterKeyError, GetMasterKeyResponse, GetOrCreateMasterKeyResult>(x =>
            {
                byte[] decryptedMasterKey = _cryptoProvider.Encryption.Decrypt(credentialKey, x.Nonce, x.EncryptedKey);
                return new GetOrCreateMasterKeyResult(decryptedMasterKey, Maybe<RecoveryKey>.None);
            })
            .MatchAsync(
                neither: Maybe<GetOrCreateMasterKeyResult>.None,
                leftAsync: async x =>
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
                    x switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
                    {
                        GetMasterKeyError.UnknownError => await Maybe<GetOrCreateMasterKeyResult>.None.AsTask(),
                        GetMasterKeyError.NotFound => await UploadNewMasterKeyAsync(versionedPassword, credentialKey)
                    },
                right: x => x);
    }

    /// <summary>
    /// Get the existing private key from the API and decrypt it.
    /// If the private key does not already exist, create and upload a new key pair.
    /// </summary>
    /// <param name="masterKey"></param>
    /// <returns></returns>
    private async Task<Maybe<GetOrCreateKeyPairResult>> GetOrCreateKeyPairAsync(byte[] masterKey)
    {
        return await _crypterApiClient.UserKey.GetPrivateKeyAsync()
            .BindAsync<GetPrivateKeyError, GetPrivateKeyResponse, GetOrCreateKeyPairResult>(x =>
            {
                byte[] decryptedPrivateKey = _cryptoProvider.Encryption.Decrypt(masterKey, x.Nonce, x.EncryptedKey);
                return new GetOrCreateKeyPairResult(decryptedPrivateKey);
            })
            .MatchAsync(
                neither: Maybe<GetOrCreateKeyPairResult>.None,
                leftAsync: async x =>
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
                    x switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
                    {
                        GetPrivateKeyError.UnkownError => await Maybe<GetOrCreateKeyPairResult>.None.AsTask(),
                        GetPrivateKeyError.NotFound => await UploadNewUserKeyPairAsync(masterKey)
                    },
                right: x => x);
    }
    
    private Task<Maybe<GetOrCreateMasterKeyResult>> UploadNewMasterKeyAsync(VersionedPassword versionedPassword, byte[] credentialKey)
    {
        byte[] newMasterKey = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.KeySize);
        byte[] nonce = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.NonceSize);
        byte[] encryptedMasterKey = _cryptoProvider.Encryption.Encrypt(credentialKey, nonce, newMasterKey);
        byte[] recoveryProof = _cryptoProvider.Random.GenerateRandomBytes(32);

        InsertMasterKeyRequest request = new InsertMasterKeyRequest(versionedPassword.Password, encryptedMasterKey, nonce, recoveryProof);
        return _crypterApiClient.UserKey.InsertMasterKeyAsync(request)
            .ToMaybeTask()
            .BindAsync(_ => new GetOrCreateMasterKeyResult(newMasterKey, new RecoveryKey(newMasterKey, request.RecoveryProof)));
    }

    private Task<Maybe<GetOrCreateKeyPairResult>> UploadNewUserKeyPairAsync(byte[] masterKey)
    {
        X25519KeyPair keyPair = _cryptoProvider.KeyExchange.GenerateKeyPair();
        byte[] nonce = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.NonceSize);
        byte[] encryptedPrivateKey = _cryptoProvider.Encryption.Encrypt(masterKey, nonce, keyPair.PrivateKey);

        InsertKeyPairRequest request = new InsertKeyPairRequest(encryptedPrivateKey, keyPair.PublicKey, nonce);
        return _crypterApiClient.UserKey.InsertKeyPairAsync(request)
            .ToMaybeTask()
            .BindAsync(_ => new GetOrCreateKeyPairResult(keyPair.PrivateKey));
    }
    
    private async Task StoreSecretKeysAsync(byte[] masterKey, byte[] privateKey, bool trustDevice)
    {
        MasterKey = masterKey;
        PrivateKey = privateKey;
        await _userKeysRepository.StoreMasterKeyAsync(masterKey, trustDevice);
        await _userKeysRepository.StorePrivateKeyAsync(privateKey, trustDevice);
    }

    private void HandleRecoveryKeyCreatedEvent(RecoveryKey recoveryKey) =>
        _recoveryKeyCreatedEventHandler?.Invoke(this, new RecoveryKeyCreatedEventArgs(recoveryKey));
    
    public event EventHandler<RecoveryKeyCreatedEventArgs> RecoveryKeyCreatedEventHandler
    {
        add => _recoveryKeyCreatedEventHandler = 
            (EventHandler<RecoveryKeyCreatedEventArgs>)Delegate.Combine(_recoveryKeyCreatedEventHandler, value);
        remove => _recoveryKeyCreatedEventHandler =
            (EventHandler<RecoveryKeyCreatedEventArgs>?)Delegate.Remove(_recoveryKeyCreatedEventHandler, value);
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
        GC.SuppressFinalize(this);
    }
    
    private sealed record GetOrCreateMasterKeyResult
    {
        public byte[] MasterKey { get; }
        public Maybe<RecoveryKey> RecoveryKey { get; }

        public GetOrCreateMasterKeyResult(byte[] masterKey, Maybe<RecoveryKey> recoveryKey)
        {
            MasterKey = masterKey;
            RecoveryKey = recoveryKey;
        }
    }

    private sealed record GetOrCreateKeyPairResult
    {
        public byte[] PrivateKey { get; }

        public GetOrCreateKeyPairResult(byte[] privateKey)
        {
            PrivateKey = privateKey;
        }
    }
}
