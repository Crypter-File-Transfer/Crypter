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
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.AccountRecovery.SubmitRecovery;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using Crypter.Crypto.Common;
using EasyMonads;

namespace Crypter.Common.Client.Services;

public class UserRecoveryService : IUserRecoveryService
{
    private readonly ICrypterApiClient _crypterApiClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IUserPasswordService _userPasswordService;

    public UserRecoveryService(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserPasswordService userPasswordService)
    {
        _crypterApiClient = crypterApiClient;
        _cryptoProvider = cryptoProvider;
        _userPasswordService = userPasswordService;
    }

    public Task RequestRecoveryEmailAsync(EmailAddress emailAddress)
    {
        return _crypterApiClient.UserRecovery.SendRecoveryEmailAsync(emailAddress);
    }

    public async Task<Either<SubmitAccountRecoveryError, Maybe<RecoveryKey>>> SubmitRecoveryRequestAsync(string recoveryCode,
        string recoverySignature, Username username, Password newPassword, Maybe<RecoveryKey> recoveryKey)
    {
        Either<SubmitAccountRecoveryError, PasswordDerivatives> passwordDerivatives = await _userPasswordService
            .DeriveUserAuthenticationPasswordAsync(username, newPassword, _userPasswordService.CurrentPasswordVersion)
            .ToEitherAsync(SubmitAccountRecoveryError.PasswordHashFailure)
            .MapAsync(async versionedPassword => await _userPasswordService.DeriveUserCredentialKeyAsync(username, newPassword, _userPasswordService.CurrentPasswordVersion)
                .ToEitherAsync(SubmitAccountRecoveryError.PasswordHashFailure)
                .BindAsync<SubmitAccountRecoveryError, byte[], PasswordDerivatives>(credentialKey => new PasswordDerivatives
                {
                    VersionedPassword = versionedPassword,
                    CredentialKey = credentialKey
                }));
        
        return await passwordDerivatives
            .Bind<RecoveryParameters>(derivatives => recoveryKey.Match(
                    () => new RecoveryParameters
                    {
                        PasswordDerivatives = derivatives
                    },
                    x =>
                    {
                        byte[] nonce = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.Encryption.NonceSize);
                        byte[] encryptedMasterKey = _cryptoProvider.Encryption.Encrypt(derivatives.CredentialKey, nonce, x.MasterKey);
                        byte[] newRecoveryProof = _cryptoProvider.Random.GenerateRandomBytes(32);
                        
                        RecoveryKey newRecoveryKey = new RecoveryKey(x.MasterKey, newRecoveryProof);
                        ReplacementMasterKeyInformation replacementMasterKeyInformation = new ReplacementMasterKeyInformation(x.Proof, newRecoveryProof, encryptedMasterKey, nonce);

                        return new RecoveryParameters
                            {
                                PasswordDerivatives = derivatives,
                                RecoveryArtifacts = new RecoveryArtifacts
                                {
                                    NewRecoveryKey = newRecoveryKey,
                                    ReplacementMasterKeyInformation = replacementMasterKeyInformation
                                }
                            };
                    }))
            .BindAsync(async x =>
            {
                AccountRecoverySubmission submission = new AccountRecoverySubmission(username.Value, recoveryCode,
                    recoverySignature, x.PasswordDerivatives.VersionedPassword, x.RecoveryArtifacts
                        .Select(y => y.ReplacementMasterKeyInformation)
                        .SomeOrDefault());
                
                return await _crypterApiClient.UserRecovery.SubmitRecoveryAsync(submission)
                    .MapAsync<SubmitAccountRecoveryError, Unit, Maybe<RecoveryKey>>(_ => x.RecoveryArtifacts
                        .Select(y => y.NewRecoveryKey));
            });
    }
    
    private struct PasswordDerivatives
    {
        public VersionedPassword VersionedPassword { get; init; }
        public byte[] CredentialKey { get; init; }
    }

    private struct RecoveryParameters
    {
        public PasswordDerivatives PasswordDerivatives { get; init; }
        public Maybe<RecoveryArtifacts> RecoveryArtifacts { get; init; }
    }

    private struct RecoveryArtifacts
    {
        public RecoveryKey NewRecoveryKey { get; init; }
        public ReplacementMasterKeyInformation ReplacementMasterKeyInformation { get; init; }
    }
}
