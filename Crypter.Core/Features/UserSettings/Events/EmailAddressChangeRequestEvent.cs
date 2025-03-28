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
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.Services;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserSettings.Events;

public record EmailAddressChangeRequestEvent(Guid UserId, Maybe<EmailAddress> NewEmailAddress) : INotification;

internal class EmailAddressChangeRequestEventHandler : INotificationHandler<EmailAddressChangeRequestEvent>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly DataContext _dataContext;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;

    public EmailAddressChangeRequestEventHandler(
        IBackgroundJobClient backgroundJobClient,
        DataContext dataContext,
        IHangfireBackgroundService hangfireBackgroundService)
    {
        _dataContext = dataContext;
        _hangfireBackgroundService = hangfireBackgroundService;
        _backgroundJobClient = backgroundJobClient;
    }
    
    public async Task Handle(EmailAddressChangeRequestEvent notification, CancellationToken cancellationToken)
    {
        await notification.NewEmailAddress.IfNoneAsync(async ()
            => await ResetNotificationSettingsAsync(notification.UserId));
        
        await _dataContext.SaveChangesAsync(CancellationToken.None);
        
        notification.NewEmailAddress.IfSome(x =>
            _backgroundJobClient.Enqueue(() =>
                _hangfireBackgroundService.SendEmailVerificationAsync(notification.UserId)));
    }

    private async Task ResetNotificationSettingsAsync(Guid userId)
    {
        UserNotificationSettingEntity? foundEntity = await _dataContext.UserNotificationSettings
            .FirstOrDefaultAsync(x => x.Owner == userId);

        if (foundEntity is not null)
        {
            foundEntity.EnableTransferNotifications = false;
            foundEntity.EmailNotifications = false;
        }
    }
}
