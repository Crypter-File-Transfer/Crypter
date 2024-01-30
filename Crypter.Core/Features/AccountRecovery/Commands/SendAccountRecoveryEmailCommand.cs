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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.DigitalSignature;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.AccountRecovery.Commands;

/// <summary>
/// Handle the first part of the account recovery process.
/// A recovery link will be emailed to the user.
/// </summary>
/// <param name="EmailAddress"></param>
public sealed record SendAccountRecoveryEmailCommand(string EmailAddress)
    : IMaybeRequest<SendAccountRecoveryEmailError>;

public enum SendAccountRecoveryEmailError
{
    Unknown,
    UserNotFound,
    InvalidSavedUsername,
    InvalidSavedEmailAddress,
    EmailFailure
}

internal sealed class SendAccountRecoveryEmailCommandHandler
    : IMaybeRequestHandler<SendAccountRecoveryEmailCommand, SendAccountRecoveryEmailError>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly DataContext _dataContext;
    private readonly IEmailService _emailService;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;
    
    private const int AccountRecoveryEmailExpirationMinutes = 30;

    public SendAccountRecoveryEmailCommandHandler(
        IBackgroundJobClient backgroundJobClient,
        ICryptoProvider cryptoProvider,
        DataContext dataContext,
        IEmailService emailService,
        IHangfireBackgroundService hangfireBackgroundService)
    {
        _backgroundJobClient = backgroundJobClient;
        _cryptoProvider = cryptoProvider;
        _dataContext = dataContext;
        _emailService = emailService;
        _hangfireBackgroundService = hangfireBackgroundService;
    }

    public async Task<Maybe<SendAccountRecoveryEmailError>> Handle(
        SendAccountRecoveryEmailCommand request, CancellationToken cancellationToken)
    {
        return await GenerateRecoveryParametersAsync(request.EmailAddress)
            .MatchAsync(
                left: error => error,
                rightAsync: async x =>
                {
                    bool emailDeliverySuccess = await _emailService.SendAccountRecoveryLinkAsync(x,
                        AccountRecoveryEmailExpirationMinutes);

                    if (!emailDeliverySuccess)
                    {
                        return SendAccountRecoveryEmailError.EmailFailure;
                    }

                    await SaveRecoveryParametersAsync(x);

                    DateTime recoveryExpiration = x.Created.DateTime.AddMinutes(AccountRecoveryEmailExpirationMinutes);
                    _backgroundJobClient.Schedule(() => _hangfireBackgroundService.DeleteRecoveryParametersAsync(x.UserId),
                        recoveryExpiration);

                    return Maybe<SendAccountRecoveryEmailError>.None;
                },
                neither: SendAccountRecoveryEmailError.Unknown);
    }

    private async Task<Either<SendAccountRecoveryEmailError, UserRecoveryParameters>> GenerateRecoveryParametersAsync(
        string emailAddress)
    {
        var userData = await _dataContext.Users
            .Where(x => x.EmailAddress == emailAddress)
            .Where(x => x.EmailVerified)
            .Select(x => new { x.Id, x.Username, x.EmailAddress })
            .FirstOrDefaultAsync();

        if (userData is null)
        {
            return SendAccountRecoveryEmailError.UserNotFound;
        }

        if (!Username.TryFrom(userData.Username, out Username username))
        {
            return SendAccountRecoveryEmailError.InvalidSavedUsername;
        }

        if (!EmailAddress.TryFrom(userData.EmailAddress!, out EmailAddress validEmailAddress))
        {
            return SendAccountRecoveryEmailError.InvalidSavedEmailAddress;
        }

        Guid recoveryCode = Guid.NewGuid();
        Ed25519KeyPair keys = _cryptoProvider.DigitalSignature.GenerateKeyPair();
        byte[] signature = Common.GenerateRecoverySignature(_cryptoProvider, keys.PrivateKey, recoveryCode, username);
        return new UserRecoveryParameters(
            userData.Id,
            username,
            validEmailAddress,
            recoveryCode,
            signature,
            keys.PublicKey,
            DateTimeOffset.UtcNow);
    }
    
    private async Task SaveRecoveryParametersAsync(UserRecoveryParameters userRecoveryParameters)
    {
        UserRecoveryEntity newEntity = new UserRecoveryEntity(userRecoveryParameters.UserId,
            userRecoveryParameters.RecoveryCode, userRecoveryParameters.VerificationKey,
            userRecoveryParameters.Created.UtcDateTime);
        _dataContext.UserRecoveries.Add(newEntity);
        await _dataContext.SaveChangesAsync();
    }
}
