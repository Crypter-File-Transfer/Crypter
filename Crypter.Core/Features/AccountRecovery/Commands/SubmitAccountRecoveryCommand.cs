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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.AccountRecovery.SubmitRecovery;
using Crypter.Common.Contracts.Features.UserConsents;
using Crypter.Common.Infrastructure;
using Crypter.Common.Primitives;
using Crypter.Core.Features.AccountRecovery.Events;
using Crypter.Core.Features.AccountRecovery.Models;
using Crypter.Core.Features.Keys;
using Crypter.Core.Identity;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.AccountRecovery.Commands;

/// <summary>
/// Handle the second part of the account recovery process.
/// The user's account will be updated with new authentication information.
/// </summary>
/// <param name="AccountRecoverySubmission"></param>
public sealed record SubmitAccountRecoveryCommand(AccountRecoverySubmission AccountRecoverySubmission)
    : IEitherRequest<SubmitAccountRecoveryError, Unit>;

internal sealed class SubmitAccountRecoveryCommandHandler
    : IEitherRequestHandler<SubmitAccountRecoveryCommand, SubmitAccountRecoveryError, Unit>
{
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IPublisher _publisher;

    public SubmitAccountRecoveryCommandHandler(
        ICryptoProvider cryptoProvider,
        DataContext dataContext,
        IPasswordHashService passwordHashService,
        IPublisher publisher)
    {
        _cryptoProvider = cryptoProvider;
        _dataContext = dataContext;
        _passwordHashService = passwordHashService;
        _publisher = publisher;
    }
    
    public Task<Either<SubmitAccountRecoveryError, Unit>> Handle(SubmitAccountRecoveryCommand request, CancellationToken cancellationToken)
    {
        return PerformRecoveryAsync(request.AccountRecoverySubmission)
            .DoRightAsync(async x =>
            {
                AccountRecoverySucceededEvent notification = new AccountRecoverySucceededEvent(x.UserId, x.DeleteUserKeys, x.DeleteUserReceivedTransfers);
                await _publisher.Publish(notification, CancellationToken.None);
            })
            .MapAsync<SubmitAccountRecoveryError, PerformRecoveryResult, Unit>(_ => Unit.Default);
    }
    
    private async Task<Either<SubmitAccountRecoveryError, PerformRecoveryResult>> PerformRecoveryAsync(
        AccountRecoverySubmission recoverySubmission)
    {
        if (!Username.TryFrom(recoverySubmission.Username, out Username validUsername))
        {
            return SubmitAccountRecoveryError.InvalidUsername;
        }

        if (!AuthenticationPassword.TryFrom(recoverySubmission.VersionedPassword.Password, out AuthenticationPassword validAuthenticationPassword))
        {
            return SubmitAccountRecoveryError.InvalidPassword;
        }
        
        UserRecoveryEntity? recoveryEntity = await _dataContext.UserRecoveries
            .Where(x => x.User!.Username == validUsername.Value)
            .FirstOrDefaultAsync();

        if (recoveryEntity is null)
        {
            return SubmitAccountRecoveryError.RecoveryNotFound;
        }

        bool validRecoveryProofProvided = false;
        if (recoverySubmission.ReplacementMasterKeyInformation?.CurrentRecoveryProof.Length > 0)
        {
            if (!MasterKeyValidators.ValidateMasterKeyInformation(
                    recoverySubmission.ReplacementMasterKeyInformation?.EncryptedKey,
                    recoverySubmission.ReplacementMasterKeyInformation?.Nonce,
                    recoverySubmission.ReplacementMasterKeyInformation?.NewRecoveryProof))
            {
                return SubmitAccountRecoveryError.InvalidMasterKey;
            }

            validRecoveryProofProvided = await _dataContext.UserMasterKeys
                .Where(x => x.Owner == recoveryEntity.Owner)
                .Where(x => x.RecoveryProof == recoverySubmission.ReplacementMasterKeyInformation!.CurrentRecoveryProof)
                .AnyAsync();

            if (!validRecoveryProofProvided)
            {
                return SubmitAccountRecoveryError.WrongRecoveryKey;
            }
        }

        Guid decodedRecoveryCode = UrlSafeEncoder.DecodeGuidFromUrlSafe(recoverySubmission.RecoveryCode);
        byte[] decodedSignature = UrlSafeEncoder.DecodeBytesFromUrlSafe(recoverySubmission.RecoverySignature);

        bool codesMatch = RecoveryCodesMatch(recoveryEntity.Code, decodedRecoveryCode);
        bool validSignature = VerifyRecoverySignature(recoveryEntity.VerificationKey, recoveryEntity.Code, validUsername, decodedSignature);

        Either<SubmitAccountRecoveryError, PerformRecoveryResult> result = codesMatch && validSignature
            ? new PerformRecoveryResult(recoveryEntity.Owner, false, false)
            : SubmitAccountRecoveryError.UnknownError;

        await result.DoRightAsync(async recoveryResult =>
        {
            UserEntity user = await _dataContext.Users
                .Include(x => x.MasterKey)
                .Where(x => x.Id == recoveryEntity.Owner)
                .FirstAsync();

            DateTime utcNow = DateTime.UtcNow;
            if (validRecoveryProofProvided)
            {
                user.MasterKey!.EncryptedKey = recoverySubmission.ReplacementMasterKeyInformation!.EncryptedKey;
                user.MasterKey!.Nonce = recoverySubmission.ReplacementMasterKeyInformation!.Nonce;
                user.MasterKey!.RecoveryProof = recoverySubmission.ReplacementMasterKeyInformation!.NewRecoveryProof;
                user.MasterKey!.Updated = utcNow;
            }
            else
            {
                recoveryResult.DeleteUserReceivedTransfers = true;
                recoveryResult.DeleteUserKeys = true;
            }

            UserConsentEntity? latestRecoveryConsent = await _dataContext.UserConsents
                .Where(x => x.Owner == user.Id)
                .Where(x => x.ConsentType == UserConsentType.RecoveryKeyRisks)
                .OrderBy(x => x.Activated)
                .FirstOrDefaultAsync();

            if (latestRecoveryConsent is not null)
            {
                latestRecoveryConsent.Active = false;
                latestRecoveryConsent.Deactivated = utcNow;
            }

            SecurePasswordHashOutput securePasswordData =
                _passwordHashService.MakeSecurePasswordHash(validAuthenticationPassword,
                    _passwordHashService.LatestServerPasswordVersion);

            user.PasswordHash = securePasswordData.Hash;
            user.PasswordSalt = securePasswordData.Salt;
            user.ServerPasswordVersion = _passwordHashService.LatestServerPasswordVersion;
            user.ClientPasswordVersion = recoverySubmission.VersionedPassword.Version;

            _dataContext.UserRecoveries.Remove(recoveryEntity);

            await _dataContext.SaveChangesAsync();
        });

        return result;
    }
    
    private bool VerifyRecoverySignature(ReadOnlySpan<byte> publicKey, Guid recoveryCode, Username username,
        ReadOnlySpan<byte> signature)
    {
        byte[] data = Common.CombineRecoveryCodeWithUsername(recoveryCode, username);
        return _cryptoProvider.DigitalSignature.VerifySignature(publicKey, data, signature);
    }
    
    private bool RecoveryCodesMatch(Guid left, Guid right)
    {
        byte[] leftBytes = left.ToByteArray();
        byte[] rightBytes = right.ToByteArray();

        return _cryptoProvider.ConstantTime.Equals(leftBytes, rightBytes);
    }
}
