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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared;

public partial class QuotaComponent
{
    [Inject] private IUserTransferSettingsService UserTransferSettingsService { get; init; } = null!;
    
    [Inject] private IUserSessionService UserSessionService { get; init; } = null!;

    private bool _loggedIn = false;
    
    private bool _freeTransferQuotaReached = false;

    private bool _userQuotaReached = false;
    
    private long _currentMaximumUploadSize = 0;
    
    private long _absoluteMaximumUploadSize = 0;

    protected override async Task OnInitializedAsync()
    {
        _loggedIn = await UserSessionService.IsLoggedInAsync();
        _freeTransferQuotaReached = await UserTransferSettingsService.IsFreeTransferQuotaReachedAsync();
        _userQuotaReached = await UserTransferSettingsService.IsUserQuotaReachedAsync();
        _currentMaximumUploadSize = await UserTransferSettingsService.GetCurrentMaximumUploadSizeAsync();
        _absoluteMaximumUploadSize = await UserTransferSettingsService.GetAbsoluteMaximumUploadSizeAsync();   
    }
}
