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
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.LinqExpressions;
using Crypter.Core.Services;
using Crypter.DataAccess;
using EasyMonads;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer;

internal static class Common
{
    private const int MaxTransferLifetimeHours = 24;
    private const int MinTransferLifetimeHours = 1;

    /// <summary>
    /// Validate whether a transfer upload may be completed with the provided parameters.
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="senderId"></param>
    /// <param name="recipientUsername"></param>
    /// <param name="itemType"></param>
    /// <param name="requestedTransferLifetimeHours"></param>
    /// <param name="ciphertextStreamLength"></param>
    /// <returns>
    /// A possible Guid in case the transfer may be completed.
    /// An error in case the transfer parameters failed validation.
    /// </returns>
    internal static async Task<Either<UploadTransferError, Maybe<Guid>>> ValidateTransferUploadAsync(
        DataContext dataContext,
        Maybe<Guid> senderId,
        Maybe<string> recipientUsername,
        TransferItemType itemType,
        int requestedTransferLifetimeHours,
        long? ciphertextStreamLength)
    {
        Maybe<Guid> recipientId = await recipientUsername
            .BindAsync(async x =>
                await GetUploadRecipientIdAsync(dataContext, senderId, x, itemType));

        if (recipientUsername.IsSome && recipientId.IsNone)
        {
            return UploadTransferError.RecipientNotFound;
        }

        if (ciphertextStreamLength.HasValue)
        {
            Maybe<Guid> transferOwner = TransferOwnershipService.GetTransferOwner(senderId, recipientId);
            bool sufficientDiskSpace = await HasSpaceForTransferAsync(dataContext, transferOwner, ciphertextStreamLength.Value);
            if (!sufficientDiskSpace)
            {
                return UploadTransferError.OutOfSpace;
            }
        }
        
        if (requestedTransferLifetimeHours is > MaxTransferLifetimeHours or < MinTransferLifetimeHours)
        {
            return UploadTransferError.InvalidRequestedLifetimeHours;
        }

        return recipientId;
    }
    
    /// <summary>
    /// Determine the TransferUserType for a transfer upload.
    /// </summary>
    /// <param name="senderId"></param>
    /// <param name="recipientId"></param>
    /// <returns></returns>
    internal static TransferUserType DetermineUploadTransferUserType(Maybe<Guid> senderId, Maybe<Guid> recipientId)
    {
        if (senderId.SomeOrDefault(Guid.Empty) != Guid.Empty)
        {
            return TransferUserType.User;
        }

        if (recipientId.SomeOrDefault(Guid.Empty) != Guid.Empty)
        {
            return TransferUserType.User;
        }

        return TransferUserType.Anonymous;
    }

    internal static async Task<Unit> QueueTransferNotificationAsync(
        DataContext dataContext,
        IBackgroundJobClient backgroundJobClient,
        IHangfireBackgroundService hangfireBackgroundService,
        Guid itemId,
        TransferItemType itemType,
        Guid recipientId)
    {
        bool userExpectsNotification = await dataContext.Users
            .Where(x => x.Id == recipientId)
            .Where(x => x.NotificationSetting!.EnableTransferNotifications && x.NotificationSetting.EmailNotifications)
            .AnyAsync();

        if (userExpectsNotification)
        {
            backgroundJobClient.Enqueue(() =>
                hangfireBackgroundService.SendTransferNotificationAsync(itemId, itemType));
        }

        return Unit.Default;
    }
    
    /// <summary>
    /// Query for a transfer recipient's user id.
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="senderId"></param>
    /// <param name="recipientUsername"></param>
    /// <param name="itemType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// The recipient's id if the recipient exists and their privacy settings permit uploads from the sender.
    /// None if the recipient does not exist or their privacy settings do not permit uploads from the sender.
    /// </returns>
    private static async Task<Maybe<Guid>> GetUploadRecipientIdAsync(
        DataContext dataContext,
        Maybe<Guid> senderId,
        string recipientUsername,
        TransferItemType itemType,
        CancellationToken cancellationToken = default)
    {
        Guid? nullableSenderId = senderId
            .Match<Guid?>(() => null, x => x);

        var recipientData = await dataContext.Users
            .Where(x => x.Username.ToLower() == recipientUsername.ToLower())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(nullableSenderId))
            .Where(LinqUserExpressions.UserPrivacyAllowsTransfer(nullableSenderId, itemType))
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return recipientData?.Id ?? Maybe<Guid>.None;
    }
    
    /// <summary>
    /// Check whether there is space to save the upload for this particular user
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="possibleUserId"></param>
    /// <param name="ciphertextStreamLength"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task<bool> HasSpaceForTransferAsync(DataContext dataContext, Maybe<Guid> possibleUserId, long ciphertextStreamLength, CancellationToken cancellationToken = default)
    {
        return await UserSettings.Common.GetUserTransferSettingsAsync(dataContext, possibleUserId, cancellationToken)
            .MatchAsync(
                () => false,
                x => Math.Min(x.MaximumUploadSize, Math.Min(x.AvailableFreeTransferSpace, x.AvailableUserSpace)) >= ciphertextStreamLength);
    }
}
