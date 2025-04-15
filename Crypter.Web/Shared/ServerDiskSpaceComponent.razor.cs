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

using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Metrics;
using Crypter.Common.Contracts.Features.Setting;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared;

public partial class ServerDiskSpaceComponent
{
    [Inject] private ClientTransferSettings UploadSettings { get; init; } = null!;

    [Inject] private ICrypterApiClient CrypterApiService { get; init; } = null!;
    
    [Inject] private ISettingService SettingService { get; init; } = null!;

    private bool _serverHasDiskSpace = true;
    
    private bool _userQuotaReached = false;

    private double _serverSpacePercentageRemaining = 100.0;

    protected override async Task OnInitializedAsync()
    {
        Task<Maybe<PublicStorageMetricsResponse>> metricsTask = CrypterApiService.Metrics.GetPublicStorageMetricsAsync();
        Task<Maybe<UploadSettings>> settingsTask = SettingService.GetUploadSettingsAsync();
        Task[] requests = [metricsTask, settingsTask];
        await Task.WhenAll(requests);
        
        _serverSpacePercentageRemaining = metricsTask.Result.Match(
            0.0,
            x => 100.0 * (x.Available / (double)x.Allocated));

        long maximumUploadSize = settingsTask.Result.Match(
            0,
            x => x.MaximumUploadSize);
        
        _serverHasDiskSpace = metricsTask.Result.Match(
            false,
            x => x.Available > maximumUploadSize);
        
        _userQuotaReached = settingsTask.Result.Match(
            true,
            x => x.TotalSpace - x.UsedSpace > maximumUploadSize);
    }
}
