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
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Core.Exceptions;
using Crypter.Core.Features.AccountRecovery.Commands;
using Crypter.Core.Features.EventLog.Commands;
using Crypter.Core.Features.Keys.Commands;
using Crypter.Core.Features.Notifications.Commands;
using Crypter.Core.Features.Reports.Commands;
using Crypter.Core.Features.Reports.Models;
using Crypter.Core.Features.Reports.Queries;
using Crypter.Core.Features.Transfer.Commands;
using Crypter.Core.Features.UserAuthentication.Commands;
using Crypter.Core.Features.UserEmailVerification.Commands;
using Crypter.Core.Features.UserToken.Commands;
using EasyMonads;
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
    /// <param name="deleteFromTransferRepository">
    /// Transfers are streamed from the transfer repository to the client.
    /// These streams are sometimes configured to "DeleteOnClose".
    /// The background service should not attempt to delete from the transfer repository when
    ///   "DeleteOnClose" is configured, otherwise a background worker might attempt to
    ///   delete the transfer from storage as the client is still streaming the transfer.
    /// </param>
    /// <returns></returns>
    Task<Unit> DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType,
        bool deleteFromTransferRepository);

    Task<Unit> DeleteUserTokenAsync(Guid tokenId);
    Task<Unit> DeleteFailedLoginAttemptAsync(Guid failedAttemptId);
    Task<Unit> DeleteRecoveryParametersAsync(Guid userId);
    Task<Unit> DeleteUserKeysAsync(Guid userId);
    Task<Unit> DeleteReceivedTransfersAsync(Guid userId);

    Task<Unit> SendApplicationAnalyticsReportAsync();
    
    Task<Unit> LogSuccessfulUserRegistrationAsync(Guid userId, string? emailAddress, string deviceDescription, DateTimeOffset timestamp);
    Task<Unit> LogFailedUserRegistrationAsync(string username, string? emailAddress, RegistrationError reason, string deviceDescription, DateTimeOffset timestamp);
    Task<Unit> LogSuccessfulUserLoginAsync(Guid userId, string deviceDescription, DateTimeOffset timestamp);
    Task<Unit> LogFailedUserLoginAsync(string username, LoginError reason, string deviceDescription, DateTimeOffset timestamp);
    Task<Unit> LogSuccessfulTransferUploadAsync(Guid itemId, TransferItemType itemType, long size, Guid? sender, string? recipient, DateTimeOffset timestamp);
    Task<Unit> LogFailedTransferUploadAsync(TransferItemType itemType, UploadTransferError reason, Guid? sender, string? recipient, DateTimeOffset timestamp);
    Task<Unit> LogSuccessfulTransferPreviewAsync(Guid itemId, TransferItemType itemType, Guid? userId, DateTimeOffset timestamp);
    Task<Unit> LogFailedTransferPreviewAsync(Guid itemId, TransferItemType itemType, Guid? userId, TransferPreviewError reason, DateTimeOffset timestamp);
    Task<Unit> LogSuccessfulTransferDownloadAsync(Guid itemId, TransferItemType itemType, Guid? userId, DateTimeOffset timestamp);
    Task<Unit> LogFailedTransferDownloadAsync(Guid itemId, TransferItemType itemType, Guid? userId, DownloadTransferCiphertextError reason, DateTimeOffset timestamp);
    Task<Unit> LogSuccessfulTransferInitializationAsync(Guid itemId, TransferItemType itemType, Guid sender, string? recipient, DateTimeOffset timestamp);
    Task<Unit> LogFailedTransferInitializationAsync(TransferItemType itemType, UploadTransferError reason, Guid sender, string? recipient, DateTimeOffset timestamp);
    Task<Unit> LogSuccessfulMultipartTransferUploadAsync(Guid itemId, TransferItemType itemType, DateTimeOffset timestamp);
    Task<Unit> LogFailedMultipartTransferUploadAsync(string hashId, TransferItemType itemType, Guid userId, UploadMultipartFileTransferError reason, DateTimeOffset timestamp);
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
    private readonly ISender _sender;
    private readonly ILogger<HangfireBackgroundService> _logger;

    public HangfireBackgroundService(ISender sender, ILogger<HangfireBackgroundService> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task<Unit> SendEmailVerificationAsync(Guid userId)
    {
        SendVerificationEmailCommand request = new SendVerificationEmailCommand(userId);
        bool success = await _sender.Send(request);
        
        if (!success)
        {
            _logger.LogError("Failed to send verification email for user: {userId}.", userId);
            throw new HangfireJobException($"{nameof(SendEmailVerificationAsync)} failed.");
        }

        return Unit.Default;
    }

    public async Task<Unit> SendTransferNotificationAsync(Guid itemId, TransferItemType itemType)
    {
        SendTransferNotificationCommand request = new SendTransferNotificationCommand(itemId, itemType);
        bool success = await _sender.Send(request);

        if (!success)
        {
            _logger.LogError("Failed to send transfer notification for item: {itemId}; type: {itemType}.",
                itemId, itemType);
            throw new HangfireJobException($"{nameof(SendTransferNotificationAsync)} failed.");
        }
        
        return Unit.Default;
    }

    public async Task<Unit> SendRecoveryEmailAsync(string emailAddress)
    {
        SendAccountRecoveryEmailCommand request = new SendAccountRecoveryEmailCommand(emailAddress);
        Maybe<SendAccountRecoveryEmailError> result = await _sender.Send(request);

        result.IfSome(error =>
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
                    _logger.LogWarning(
                        "A user was found to have an invalid username while attempting to send a recovery email.");
                    break;
                case SendAccountRecoveryEmailError.InvalidSavedEmailAddress:
                    _logger.LogWarning(
                        "A user was found to have an invalid email address while attempting to send a recovery email.");
                    break;
                case SendAccountRecoveryEmailError.EmailFailure:
                    _logger.LogWarning("An email failure occurred while trying to send a recovery email.");
                    throw new HangfireJobException($"{nameof(SendRecoveryEmailAsync)} failed.");
            }
        });
        
        return Unit.Default;
    }

    public Task<Unit> DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType, bool deleteFromTransferRepository)
    {
        DeleteTransferCommand request = new DeleteTransferCommand(itemId, itemType, userType, deleteFromTransferRepository);
        return _sender.Send(request);
    }

    public Task<Unit> DeleteUserTokenAsync(Guid tokenId)
    {
        DeleteUserTokenCommand request = new DeleteUserTokenCommand(tokenId);
        return _sender.Send(request);
    }

    public Task<Unit> DeleteFailedLoginAttemptAsync(Guid failedAttemptId)
    {
        DeleteFailedLoginAttemptCommand request = new DeleteFailedLoginAttemptCommand(failedAttemptId);
        return _sender.Send(request);
    }

    public async Task<Unit> DeleteRecoveryParametersAsync(Guid userId)
    {
        DeleteRecoveryParametersCommand request = new DeleteRecoveryParametersCommand(userId);
        return await _sender.Send(request);
    }

    public async Task<Unit> DeleteUserKeysAsync(Guid userId)
    {
        DeleteUserKeysCommand request = new DeleteUserKeysCommand(userId);
        return await _sender.Send(request);
    }

    public Task<Unit> DeleteReceivedTransfersAsync(Guid userId)
    {
        DeleteUserReceivedTransfersCommand request = new DeleteUserReceivedTransfersCommand(userId);
        return _sender.Send(request);
    }

    public async Task<Unit> SendApplicationAnalyticsReportAsync()
    {
        ApplicationAnalyticsReportQuery reportRequest = new ApplicationAnalyticsReportQuery(7);
        ApplicationAnalyticsReport report = await _sender.Send(reportRequest);
        
        SendApplicationsAnalyticsReportCommand emailRequest = new SendApplicationsAnalyticsReportCommand(report);
        bool success = await _sender.Send(emailRequest);

        if (!success)
        {
            _logger.LogError("Failed to send application analytics report.");
            throw new HangfireJobException($"{nameof(SendApplicationAnalyticsReportAsync)} failed.");
        }
        
        return Unit.Default;
    }
    
    public Task<Unit> LogSuccessfulUserRegistrationAsync(Guid userId, string? emailAddress, string deviceDescription, DateTimeOffset timestamp)
    {
        LogSuccessfulUserRegistrationCommand request = new LogSuccessfulUserRegistrationCommand(userId, emailAddress, deviceDescription, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogFailedUserRegistrationAsync(string username, string? emailAddress, RegistrationError reason, string deviceDescription, DateTimeOffset timestamp)
    {
        LogFailedUserRegistrationCommand request = new LogFailedUserRegistrationCommand(username, emailAddress, reason, deviceDescription, timestamp);
        return _sender.Send(request);
    }
    
    public Task<Unit> LogSuccessfulUserLoginAsync(Guid userId, string deviceDescription, DateTimeOffset timestamp)
    {
        LogSuccessfulUserLoginCommand request = new LogSuccessfulUserLoginCommand(userId, deviceDescription, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogFailedUserLoginAsync(string username, LoginError reason, string deviceDescription, DateTimeOffset timestamp)
    {
        LogFailedUserLoginCommand request = new LogFailedUserLoginCommand(username, reason, deviceDescription, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogSuccessfulTransferUploadAsync(Guid itemId, TransferItemType itemType, long size, Guid? sender, string? recipient, DateTimeOffset timestamp)
    {
        LogSuccessfulTransferUploadCommand request = new LogSuccessfulTransferUploadCommand(itemId, itemType, size, sender, recipient, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogFailedTransferUploadAsync(TransferItemType itemType, UploadTransferError reason, Guid? sender, string? recipient, DateTimeOffset timestamp)
    {
        LogFailedTransferUploadCommand request = new LogFailedTransferUploadCommand(itemType, reason, sender, recipient, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogSuccessfulTransferPreviewAsync(Guid itemId, TransferItemType itemType, Guid? userId, DateTimeOffset timestamp)
    {
        LogSuccessfulTransferPreviewCommand request = new LogSuccessfulTransferPreviewCommand(itemId, itemType, userId, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogFailedTransferPreviewAsync(Guid itemId, TransferItemType itemType, Guid? userId, TransferPreviewError reason, DateTimeOffset timestamp)
    {
        LogFailedTransferPreviewCommand request = new LogFailedTransferPreviewCommand(itemId, itemType, userId, reason, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogSuccessfulTransferDownloadAsync(Guid itemId, TransferItemType itemType, Guid? userId, DateTimeOffset timestamp)
    {
        LogSuccessfulTransferDownloadCommand request = new LogSuccessfulTransferDownloadCommand(itemId, itemType, userId, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogFailedTransferDownloadAsync(Guid itemId, TransferItemType itemType, Guid? userId, DownloadTransferCiphertextError reason, DateTimeOffset timestamp)
    {
        LogFailedTransferDownloadCommand request = new LogFailedTransferDownloadCommand(itemId, itemType, userId, reason, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogSuccessfulTransferInitializationAsync(Guid itemId, TransferItemType itemType, Guid sender, string? recipient, DateTimeOffset timestamp)
    {
        LogSuccessfulMultipartTransferInitializationCommand request = new LogSuccessfulMultipartTransferInitializationCommand(itemId, itemType, sender, recipient, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogFailedTransferInitializationAsync(TransferItemType itemType, UploadTransferError reason, Guid sender, string? recipient, DateTimeOffset timestamp)
    {
        LogFailedMultipartTransferInitializationCommand request = new LogFailedMultipartTransferInitializationCommand(itemType, reason, sender, recipient, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogSuccessfulMultipartTransferUploadAsync(Guid itemId, TransferItemType itemType, DateTimeOffset timestamp)
    {
        LogSuccessfulMultipartTransferUploadCommand request = new LogSuccessfulMultipartTransferUploadCommand(itemId, itemType, timestamp);
        return _sender.Send(request);
    }

    public Task<Unit> LogFailedMultipartTransferUploadAsync(string hashId, TransferItemType itemType, Guid userId, UploadMultipartFileTransferError reason, DateTimeOffset timestamp)
    {
        LogFailedMultipartTransferUploadCommand request = new LogFailedMultipartTransferUploadCommand(hashId, itemType, userId, reason, timestamp);
        return _sender.Send(request);
    }
}
