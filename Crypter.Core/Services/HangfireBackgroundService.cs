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
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Core.Extensions;
using Crypter.Core.Features.Keys.Commands;
using Crypter.Core.Features.Transfer;
using Crypter.Core.Features.UserAuthentication;
using Crypter.Core.Features.UserEmailVerification;
using Crypter.Core.Features.UserRecovery;
using Crypter.Core.Features.UserToken;
using Crypter.Core.Repositories;
using Crypter.Crypto.Common;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Services;

public interface IHangfireBackgroundService
{
    Task SendEmailVerificationAsync(Guid userId);
    Task SendTransferNotificationAsync(Guid itemId, TransferItemType itemType);
    Task SendRecoveryEmailAsync(string emailAddress);

    /// <summary>
    /// Delete a transfer from transfer storage and the database.
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="itemType"></param>
    /// <param name="userType"></param>
    /// <param name="deleteFromTransferStorage">
    /// Transfers are streamed from transfer storage to the client.
    /// These streams are sometimes configured to "DeleteOnClose".
    /// The background service should not delete from transfer storage when "DeleteOnClose" is configured.
    /// </param>
    /// <returns></returns>
    Task DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType,
        bool deleteFromTransferStorage);

    Task DeleteUserTokenAsync(Guid tokenId);
    Task DeleteFailedLoginAttemptAsync(Guid failedAttemptId);
    Task DeleteRecoveryParametersAsync(Guid userId);
    Task<Unit> DeleteUserKeysAsync(Guid userId);
}

/// <summary>
/// The purpose of this class is to organize all the methods invoked by Hangfire.
/// Modifying the method definitions risks breaking pending jobs in production.
/// Moving methods out of this class also risks breaking pending jobs in production.
/// 
/// Do not modify methods in this class or move them out of the class.
/// </summary>
public class HangfireBackgroundService : IHangfireBackgroundService
{
    private readonly DataContext _dataContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ICryptoProvider _cryptoProvider;
    private readonly IEmailService _emailService;
    private readonly ITransferRepository _transferRepository;

    private const int _accountRecoveryEmailExpirationMinutes = 30;

    public HangfireBackgroundService(
        DataContext dataContext, IBackgroundJobClient backgroundJobClient, ICryptoProvider cryptoProvider,
        IEmailService emailService, ITransferRepository transferRepository)
    {
        _dataContext = dataContext;
        _backgroundJobClient = backgroundJobClient;
        _cryptoProvider = cryptoProvider;
        _emailService = emailService;
        _transferRepository = transferRepository;
    }

    public Task SendEmailVerificationAsync(Guid userId)
    {
        return UserEmailVerificationQueries.GenerateVerificationParametersAsync(_dataContext, _cryptoProvider, userId)
            .IfSomeAsync(async x =>
            {
                bool deliverySuccess = await _emailService.SendEmailVerificationAsync(x);
                if (deliverySuccess)
                {
                    await UserEmailVerificationCommands.SaveVerificationParametersAsync(_dataContext, x);
                }
            });
    }

    public async Task SendTransferNotificationAsync(Guid itemId, TransferItemType itemType)
    {
        UserEntity recipient = null;

        switch (itemType)
        {
            case TransferItemType.Message:
                recipient = await _dataContext.UserMessageTransfers
                    .Where(x => x.Id == itemId)
                    .Select(x => x.Recipient)
                    .Where(LinqUserExpressions.UserReceivesEmailNotifications())
                    .FirstOrDefaultAsync();
                break;
            case TransferItemType.File:
                recipient = await _dataContext.UserFileTransfers
                    .Where(x => x.Id == itemId)
                    .Select(x => x.Recipient)
                    .Where(LinqUserExpressions.UserReceivesEmailNotifications())
                    .FirstOrDefaultAsync();
                break;
        }

        if (recipient is not null
            && EmailAddress.TryFrom(recipient.EmailAddress, out EmailAddress validEmailAddress))
        {
            await _emailService.SendTransferNotificationAsync(validEmailAddress);
        }
    }

    public Task SendRecoveryEmailAsync(string emailAddress)
    {
        return UserRecoveryQueries.GenerateRecoveryParametersAsync(_dataContext, _cryptoProvider, emailAddress)
            .IfSomeAsync(async parameters =>
            {
                bool deliverySuccess =
                    await _emailService.SendAccountRecoveryLinkAsync(parameters,
                        _accountRecoveryEmailExpirationMinutes);
                if (deliverySuccess)
                {
                    DateTime utcNow = DateTime.UtcNow;
                    await UserRecoveryCommands.SaveRecoveryParametersAsync(_dataContext, parameters, utcNow);

                    DateTime recoveryExpiration = utcNow.AddMinutes(_accountRecoveryEmailExpirationMinutes);
                    _backgroundJobClient.Schedule(() => DeleteRecoveryParametersAsync(parameters.UserId),
                        recoveryExpiration);
                }
            });
    }

    public Task DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType,
        bool deleteFromTransferStorage)
    {
        return TransferCommands.DeleteTransferAsync(_dataContext, _transferRepository, itemId, itemType, userType,
            deleteFromTransferStorage);
    }

    public Task DeleteUserTokenAsync(Guid tokenId)
    {
        return UserTokenCommands.DeleteUserTokenAsync(_dataContext, tokenId);
    }

    public Task DeleteFailedLoginAttemptAsync(Guid failedAttemptId)
    {
        return UserAuthenticationCommands.DeleteFailedLoginAttemptAsync(_dataContext, failedAttemptId);
    }

    public Task DeleteRecoveryParametersAsync(Guid userId)
    {
        return UserRecoveryCommands.DeleteRecoveryParametersAsync(_dataContext, userId);
    }

    public Task<Unit> DeleteUserKeysAsync(Guid userId)
    {
        return DeleteUserKeysCommandHandler.HandleAsync(_dataContext, userId);
    }
}
