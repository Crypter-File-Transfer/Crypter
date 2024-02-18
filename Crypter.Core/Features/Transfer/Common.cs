using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Core.Features.Metrics.Queries;
using Crypter.Core.LinqExpressions;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Crypter.DataAccess;
using EasyMonads;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer;

internal static class Common
{
    internal const int MaxTransferLifetimeHours = 24;
    internal const int MinTransferLifetimeHours = 1;

    /// <summary>
    /// Validate whether a transfer upload may be completed with the provided parameters.
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="transferStorageSettings"></param>
    /// <param name="senderId"></param>
    /// <param name="recipientUsername"></param>
    /// <param name="requestedTransferLifetimeHours"></param>
    /// <param name="ciphertextStreamLength"></param>
    /// <returns>
    /// A possible Guid in case the transfer may be completed.
    /// An error in case the transfer parameters failed validation.
    /// </returns>
    internal static async Task<Either<UploadTransferError, Maybe<Guid>>> ValidateTransferUploadAsync(
        DataContext dataContext,
        TransferStorageSettings transferStorageSettings,
        Maybe<Guid> senderId,
        Maybe<string> recipientUsername,
        int requestedTransferLifetimeHours,
        long ciphertextStreamLength)
    {
        Maybe<Guid> recipientId = await recipientUsername
            .BindAsync(async x =>
                await GetUploadRecipientIdAsync(dataContext, senderId, x));

        if (recipientUsername.IsSome && recipientId.IsNone)
        {
            return UploadTransferError.RecipientNotFound;
        }

        bool sufficientDiskSpace =
            await HasSpaceForTransferAsync(dataContext, transferStorageSettings, ciphertextStreamLength);
        if (!sufficientDiskSpace)
        {
            return UploadTransferError.OutOfSpace;
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
        IBackgroundJobClient backgroundJobClient,
        DataContext dataContext,
        IHangfireBackgroundService hangfireBackgroundService,
        Guid itemId,
        TransferItemType itemType,
        Maybe<Guid> maybeUserId)
    {
        await maybeUserId.IfSomeAsync(
            async userId =>
            {
                bool userExpectsNotification = await dataContext.Users
                    .Where(x => x.Id == userId)
                    .Where(x => x.NotificationSetting!.EnableTransferNotifications
                                && x.EmailVerified
                                && x.NotificationSetting.EmailNotifications)
                    .AnyAsync();

                if (userExpectsNotification)
                {
                    backgroundJobClient.Enqueue(() =>
                        hangfireBackgroundService.SendTransferNotificationAsync(itemId, itemType));
                }
            });
        return Unit.Default;
    }

    internal static string ScheduleTransferDeletion(
        IBackgroundJobClient backgroundJobClient,
        IHangfireBackgroundService hangfireBackgroundService,
        Guid itemId, TransferItemType itemType,
        TransferUserType userType,
        DateTime itemExpiration)
        => backgroundJobClient.Schedule(
            () => hangfireBackgroundService.DeleteTransferAsync(itemId, itemType, userType, true), itemExpiration);
    
    /// <summary>
    /// Query for a transfer recipient's user id.
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="senderId"></param>
    /// <param name="recipientUsername"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// The recipient's id if the recipient exists and their privacy settings permit uploads from the sender.
    /// None if the recipient does not exist or their privacy settings do not permit uploads from the sender.
    /// </returns>
    private static async Task<Maybe<Guid>> GetUploadRecipientIdAsync(
        DataContext dataContext,
        Maybe<Guid> senderId,
        string recipientUsername,
        CancellationToken cancellationToken = default)
    {
        Guid? nullableSenderId = senderId
            .Match<Guid?>(() => null, x => x);

        var recipientData = await dataContext.Users
            .Where(x => x.Username.ToLower() == recipientUsername.ToLower())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(nullableSenderId))
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return recipientData?.Id ?? Maybe<Guid>.None;
    }
    
    /// <summary>
    /// Check whether there is sufficient disk space to save the transfer upload.
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="transferStorageSettings"></param>
    /// <param name="ciphertextStreamLength"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task<bool> HasSpaceForTransferAsync(
        DataContext dataContext,
        TransferStorageSettings transferStorageSettings,
        long ciphertextStreamLength,
        CancellationToken cancellationToken = default)
    {
        GetDiskMetricsResult diskMetrics = await Metrics.Common.GetDiskMetricsAsync(
            dataContext,
            transferStorageSettings,
            cancellationToken);

        return ciphertextStreamLength <= diskMetrics.FreeBytes;
    }
}
