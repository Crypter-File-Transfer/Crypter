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
using Crypter.Common.Enums;
using Crypter.Core.Services;
using Crypter.DataAccess;
using EasyMonads;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.Transfer.Events;

public sealed record SuccessfulTransferUploadEvent(Guid ItemId, TransferItemType ItemType, TransferUserType UserType, long Size, Maybe<Guid> SenderId, Maybe<Guid> RecipientId, Maybe<string> RecipientName, DateTimeOffset ItemExpiration, DateTimeOffset Timestamp) : INotification;

internal sealed class SuccessfulTransferUploadEventHandler : INotificationHandler<SuccessfulTransferUploadEvent>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly DataContext _dataContext;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;
    
    public SuccessfulTransferUploadEventHandler(
        IBackgroundJobClient backgroundJobClient,
        DataContext dataContext,
        IHangfireBackgroundService hangfireBackgroundService)
    {
        _backgroundJobClient = backgroundJobClient;
        _dataContext = dataContext;
        _hangfireBackgroundService = hangfireBackgroundService;
    }
    
    public async Task Handle(SuccessfulTransferUploadEvent notification, CancellationToken cancellationToken)
    {
        await notification.RecipientId.IfSomeAsync(async recipientId =>
            await QueueTransferNotificationAsync(notification.ItemId, notification.ItemType, recipientId));
        
        _backgroundJobClient.Schedule(() =>
            _hangfireBackgroundService.DeleteTransferAsync(notification.ItemId, notification.ItemType, notification.UserType, true),
            notification.ItemExpiration);

        Guid? senderId = notification.SenderId
            .Match((Guid?)null, x => x);

        string? recipientUsername = notification.RecipientName
            .Match((string?)null, x => x);
        
        _backgroundJobClient.Enqueue(() =>
            _hangfireBackgroundService.LogSuccessfulTransferUploadAsync(notification.ItemId, notification.ItemType, notification.Size, senderId, recipientUsername, notification.Timestamp));
    }
    
    private async Task<Unit> QueueTransferNotificationAsync(
        Guid itemId,
        TransferItemType itemType,
        Guid recipientId)
    {
        bool userExpectsNotification = await _dataContext.Users
            .Where(x => x.Id == recipientId)
            .Where(x => x.NotificationSetting!.EnableTransferNotifications
                        && x.EmailVerified
                        && x.NotificationSetting.EmailNotifications)
            .AnyAsync();

        if (userExpectsNotification)
        {
            _backgroundJobClient.Enqueue(() =>
                _hangfireBackgroundService.SendTransferNotificationAsync(itemId, itemType));
        }

        return Unit.Default;
    }
}
