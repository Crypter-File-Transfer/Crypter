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
using Crypter.Common.Contracts.Features.UserSettings.PrivacySettings;
using Crypter.Core.MediatorMonads;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserSettings.Commands;

public sealed record UpdatePrivacySettingsCommand(Guid UserId, PrivacySettings Request)
    : IEitherRequest<SetPrivacySettingsError, PrivacySettings>;

internal class UpdatePrivacySettingsCommandHandler
    : IEitherRequestHandler<UpdatePrivacySettingsCommand, SetPrivacySettingsError, PrivacySettings>
{
    private readonly DataContext _dataContext;

    public UpdatePrivacySettingsCommandHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<Either<SetPrivacySettingsError, PrivacySettings>> Handle(UpdatePrivacySettingsCommand request, CancellationToken cancellationToken)
    {
        UserPrivacySettingEntity? userPrivacySettings = await _dataContext.UserPrivacySettings
            .FirstOrDefaultAsync(x => x.Owner == request.UserId, CancellationToken.None);

        if (userPrivacySettings is null)
        {
            UserPrivacySettingEntity newPrivacySettings = new UserPrivacySettingEntity(
                request.UserId,
                request.Request.AllowKeyExchangeRequests,
                request.Request.VisibilityLevel, 
                request.Request.FileTransferPermission, 
                request.Request.MessageTransferPermission);
            
            _dataContext.UserPrivacySettings.Add(newPrivacySettings);
        }
        else
        {
            userPrivacySettings.AllowKeyExchangeRequests = request.Request.AllowKeyExchangeRequests;
            userPrivacySettings.Visibility = request.Request.VisibilityLevel;
            userPrivacySettings.ReceiveFiles = request.Request.FileTransferPermission;
            userPrivacySettings.ReceiveMessages = request.Request.MessageTransferPermission;
        }

        await _dataContext.SaveChangesAsync(CancellationToken.None);
        
        return await Common.GetPrivacySettingsAsync(_dataContext, request.UserId, cancellationToken)
            .ToEitherAsync(SetPrivacySettingsError.UnknownError);
    }
}
