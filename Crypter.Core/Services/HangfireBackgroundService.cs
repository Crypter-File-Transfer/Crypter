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
using System.Threading.Tasks;
using Crypter.Common.Enums;
using Crypter.Core.Exceptions;
using Crypter.Core.Features.AccountRecovery.Commands;
using Crypter.Core.Features.Keys.Commands;
using Crypter.Core.Features.Notifications.Commands;
using Crypter.Core.Features.Transfer.Commands;
using Crypter.Core.Features.UserAuthentication;
using Crypter.Core.Features.UserEmailVerification.Commands;
using Crypter.Core.Features.UserToken;
using Crypter.Core.Models;
using Crypter.DataAccess;
using EasyMonads;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Services;

public interface IHangfireBackgroundService
{
    Task<Unit> SendEmailVerificationAsync(Guid userId);
    Task<Unit> SendTransferNotificationAsync(Guid itemId, TransferItemType itemType);
    Task<Unit> SendRecoveryEmailAsync(string emailAddress);

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
    Task<Unit> DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType,
        bool deleteFromTransferStorage);

    Task DeleteUserTokenAsync(Guid tokenId);
    Task DeleteFailedLoginAttemptAsync(Guid failedAttemptId);
    Task<Unit> DeleteRecoveryParametersAsync(Guid userId);
    Task<Unit> DeleteUserKeysAsync(Guid userId);
    Task<Unit> DeleteReceivedTransfersAsync(Guid userId);
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
    private readonly ISender _sender;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<HangfireBackgroundService> _logger;

    private const int AccountRecoveryEmailExpirationMinutes = 30;

    public HangfireBackgroundService(
        DataContext dataContext,
        ISender sender,
        IBackgroundJobClient backgroundJobClient,
        ILogger<HangfireBackgroundService> logger)
    {
        _dataContext = dataContext;
        _sender = sender;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<Unit> SendEmailVerificationAsync(Guid userId)
    {
        SendVerificationEmailCommand request = new SendVerificationEmailCommand(userId);
        bool result = await _sender.Send(request);
        
        if (!result)
        {
            _logger.LogError("Failed to send verification email for user: {userId}.", userId);
            throw new HangfireJobException($"{nameof(SendEmailVerificationAsync)} failed.");
        }

        return Unit.Default;
    }

    public async Task<Unit> SendTransferNotificationAsync(Guid itemId, TransferItemType itemType)
    {
        SendTransferNotificationCommand request = new SendTransferNotificationCommand(itemId, itemType);
        bool result = await _sender.Send(request);

        if (!result)
        {
            _logger.LogError("Failed to send transfer notification for item: {itemId}; type: {itemType}.",
                itemId, itemType);
            throw new HangfireJobException($"{nameof(SendTransferNotificationAsync)} failed.");
        }
        
        return Unit.Default;
    }

    public async Task<Unit> SendRecoveryEmailAsync(string emailAddress)
    {
        SendAccountRecoveryEmailCommand request =
            new SendAccountRecoveryEmailCommand(emailAddress, AccountRecoveryEmailExpirationMinutes);
        Either<SendAccountRecoveryEmailError, UserRecoveryParameters> result = await _sender.Send(request);
        
        result.DoRight(x =>
        {
            DateTime recoveryExpiration = x.Created.DateTime.AddMinutes(AccountRecoveryEmailExpirationMinutes);
            _backgroundJobClient.Schedule(() => DeleteRecoveryParametersAsync(x.UserId),
                recoveryExpiration);
        }).DoLeftOrNeither(
            left: error =>
            {
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (error)
                {
                    case SendAccountRecoveryEmailError.Unknown:
                        _logger.LogError("An unknown error occurred while trying to send a recovery email.");
                        throw new HangfireJobException($"{nameof(SendRecoveryEmailAsync)} failed.");
                    case SendAccountRecoveryEmailError.UserNotFound:
                        _logger.LogWarning("A user was not found while attempting to send a recovery email.");
                        break;
                    case SendAccountRecoveryEmailError.InvalidSavedUsername:
                        _logger.LogWarning("A user was found to have an invalid username while attempting to send a recovery email.");
                        break;
                    case SendAccountRecoveryEmailError.InvalidSavedEmailAddress:
                        _logger.LogWarning("A user was found to have an invalid email address while attempting to send a recovery email.");
                        break;
                    case SendAccountRecoveryEmailError.EmailFailure:
                        _logger.LogWarning("An email failure occurred while trying to send a recovery email.");
                        throw new HangfireJobException($"{nameof(SendRecoveryEmailAsync)} failed.");
                }
            },
            neither: () =>
            {
                _logger.LogError("Something unforeseen occurred while trying to send a recovery email.");
                throw new HangfireJobException($"{nameof(SendRecoveryEmailAsync)} failed.");
            });
        
        return Unit.Default;
    }

    public Task<Unit> DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType,
        bool deleteFromTransferStorage)
    {
        DeleteTransferCommand request = new DeleteTransferCommand(itemId, itemType, userType, deleteFromTransferStorage);
        return _sender.Send(request);
    }

    public Task DeleteUserTokenAsync(Guid tokenId)
    {
        return UserTokenCommands.DeleteUserTokenAsync(_dataContext, tokenId);
    }

    public Task DeleteFailedLoginAttemptAsync(Guid failedAttemptId)
    {
        return UserAuthenticationCommands.DeleteFailedLoginAttemptAsync(_dataContext, failedAttemptId);
    }

    public async Task<Unit> DeleteRecoveryParametersAsync(Guid userId)
    {
        DeleteRecoveryParametersCommand request = new DeleteRecoveryParametersCommand(userId);
        return await _sender.Send(request);
    }

    public async Task<Unit> DeleteUserKeysAsync(Guid userId)
    {
        var request = new DeleteUserKeysCommand(userId);
        return await _sender.Send(request);
    }

    public Task<Unit> DeleteReceivedTransfersAsync(Guid userId)
    {
        DeleteUserReceivedTransfersCommand request = new DeleteUserReceivedTransfersCommand(userId);
        return _sender.Send(request);
    }
}
