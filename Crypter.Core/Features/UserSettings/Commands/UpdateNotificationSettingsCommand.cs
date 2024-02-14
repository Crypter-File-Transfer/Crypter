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
using Crypter.Common.Contracts.Features.UserSettings.NotificationSettings;
using Crypter.Core.MediatorMonads;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserSettings.Commands;

public sealed record UpdateNotificationSettingsCommand(Guid UserId, NotificationSettings Request)
    : IEitherRequest<UpdateNotificationSettingsError, NotificationSettings>;

internal class UpdateNotificationSettingsCommandHandler
    : IEitherRequestHandler<UpdateNotificationSettingsCommand, UpdateNotificationSettingsError, NotificationSettings>
{
    private readonly DataContext _dataContext;

    public UpdateNotificationSettingsCommandHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }


    public async Task<Either<UpdateNotificationSettingsError, NotificationSettings>> Handle(UpdateNotificationSettingsCommand request, CancellationToken cancellationToken)
    {
        var userData = await _dataContext.Users
            .Where(x => x.Id == request.UserId)
            .Select(x => new { x.EmailVerified, x.NotificationSetting })
            .FirstOrDefaultAsync(CancellationToken.None);

        if (userData is null)
        {
            return UpdateNotificationSettingsError.UnknownError;
        }

        if (!userData.EmailVerified && request.Request.EmailNotifications)
        {
            return UpdateNotificationSettingsError.EmailAddressNotVerified;
        }

        if (userData.NotificationSetting is null)
        {
            UserNotificationSettingEntity newNotificationSettings =
                new UserNotificationSettingEntity(
                    request.UserId,
                    request.Request.NotifyOnTransferReceived,
                    request.Request.EmailNotifications);
            
            _dataContext.UserNotificationSettings.Add(newNotificationSettings);
        }
        else
        {
            userData.NotificationSetting.EmailNotifications = request.Request.EmailNotifications;
            userData.NotificationSetting.EnableTransferNotifications = request.Request.NotifyOnTransferReceived;
        }

        await _dataContext.SaveChangesAsync(CancellationToken.None);

        return await Common.GetUserNotificationSettingsAsync(_dataContext, request.UserId, cancellationToken)
            .ToEitherAsync(UpdateNotificationSettingsError.UnknownError);
    }
}
    
