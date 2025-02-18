﻿/*
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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.KeyExchange;
using EasyMonads;
using Microsoft.Extensions.Logging;

namespace Crypter.Common.Client.Services;

public class UserKeysService : IUserKeysService
{
    private readonly ILogger<UserKeysService> _logger;
    private readonly ICrypterApiClient _crypterApiClient;
    private readonly ICryptoProvider _cryptoProvider;
    protected readonly IUserPasswordService UserPasswordService;
    protected readonly IUserKeysRepository UserKeysRepository;
    private Maybe<byte[]> _masterKey = Maybe<byte[]>.None;
    private Maybe<byte[]> _privateKey = Maybe<byte[]>.None;

    public Maybe<byte[]> MasterKey
    {
        get
        {
            _logger.LogDebug("Getting MasterKey. IsSome: {isSome}", _masterKey.IsSome);
            return _masterKey;
        }
        protected set
        {
            _logger.LogDebug("Setting MasterKey. IsSome: {isSome}", value.IsSome);
            _masterKey = value;
        }
    }

    public Maybe<byte[]> PrivateKey
    {
        get
        {
            _logger.LogDebug("Getting PrivateKey. IsSome: {isSome}", _privateKey.IsSome);
            return _privateKey;
        }
        protected set
        {
            _logger.LogDebug("Setting PrivateKey. IsSome: {isSome}", value.IsSome);
            _privateKey = value;
        }
    }

    public UserKeysService(ILogger<UserKeysService> logger, ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserPasswordService userPasswordService, IUserKeysRepository userKeysRepository)
    {
        _logger = logger;
        _crypterApiClient = crypterApiClient;
        UserPasswordService = userPasswordService;
        _cryptoProvider = cryptoProvider;
        UserKeysRepository = userKeysRepository;
    }
    
    public Task<Maybe<RecoveryKey>> DeriveRecoveryKeyAsync(byte[] masterKey, Username username, Password password)
    {
        return UserPasswordService
            .DeriveUserAuthenticationPasswordAsync(username, password, UserPasswordService.CurrentPasswordVersion)
            .BindAsync(versionedPassword => DeriveRecoveryKeyAsync(masterKey, versionedPassword));
    }

    public Task<Maybe<RecoveryKey>> DeriveRecoveryKeyAsync(byte[] masterKey, VersionedPassword versionedPassword)
    {
        GetMasterKeyRecoveryProofRequest request = new GetMasterKeyRecoveryProofRequest(versionedPassword.Password);
        return _crypterApiClient.UserKey.GetMasterKeyRecoveryProofAsync(request)
            .ToMaybeTask()
            .MapAsync(x => new RecoveryKey(masterKey, x.Proof));
    }
    
    /// <summary>
    /// Get the existing master key from the API and decrypt it.
    /// If the master key does not already exist, create and upload a new one.
    /// </summary>
    /// <param name="versionedPassword"></param>
    /// <param name="credentialKey"></param>
    /// <returns></returns>
    protected async Task<Maybe<GetOrCreateMasterKeyResult>> GetOrCreateMasterKeyAsync(VersionedPassword versionedPassword, byte[] credentialKey)
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
    protected async Task<Maybe<GetOrCreateKeyPairResult>> GetOrCreateKeyPairAsync(byte[] masterKey)
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
    
    protected async Task StoreSecretKeysAsync(byte[] masterKey, byte[] privateKey, bool trustDevice)
    {
        _logger.LogDebug("Storing secret keys");
        MasterKey = masterKey;
        PrivateKey = privateKey;
        await UserKeysRepository.StoreMasterKeyAsync(masterKey, trustDevice);
        await UserKeysRepository.StorePrivateKeyAsync(privateKey, trustDevice);
    }
    
    protected sealed record GetOrCreateMasterKeyResult
    {
        public byte[] MasterKey { get; }
        public Maybe<RecoveryKey> NewRecoveryKey { get; }

        public GetOrCreateMasterKeyResult(byte[] masterKey, Maybe<RecoveryKey> newRecoveryKey)
        {
            MasterKey = masterKey;
            NewRecoveryKey = newRecoveryKey;
        }
    }

    protected sealed record GetOrCreateKeyPairResult
    {
        public byte[] PrivateKey { get; }

        public GetOrCreateKeyPairResult(byte[] privateKey)
        {
            PrivateKey = privateKey;
        }
    }
}
