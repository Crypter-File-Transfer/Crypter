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
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.TransferSettings;
using EasyMonads;
using Microsoft.Extensions.Caching.Memory;

namespace Crypter.Common.Client.Services.UserSettings;

public class UserTransferUserTransferSettingsService : IUserTransferSettingsService
{
    private readonly IUserSessionService _userSessionService;
    private readonly ICrypterApiClient _crypterApiClient;
    
    private readonly IMemoryCache _memoryCache;
    
    public UserTransferUserTransferSettingsService(IUserSessionService userSessionService, ICrypterApiClient crypterApiClient, IMemoryCache memoryCache)
    {
        _userSessionService = userSessionService;
        _crypterApiClient = crypterApiClient;
        _memoryCache = memoryCache;
    }
    
    public async Task<Maybe<GetTransferSettingsResponse>> GetTransferSettingsAsync()
    {
        const string cacheKey = $"{nameof(UserTransferUserTransferSettingsService)}:{nameof(GetTransferSettingsAsync)}";
        return await _memoryCache.GetOrCreateAsync<GetTransferSettingsResponse?>(cacheKey, async entry =>
        {
            bool isLoggedIn = await _userSessionService.IsLoggedInAsync();
            Maybe<GetTransferSettingsResponse> uploadSettings = await _crypterApiClient.UserSetting.GetTransferSettingsAsync(isLoggedIn);
            return uploadSettings
                .IfNone(() => entry.SetValue(null))
                .Bind<GetTransferSettingsResponse?>(x => x)
                .SomeOrDefault(null);
        }, new MemoryCacheEntryOptions{AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)}) ?? Maybe<GetTransferSettingsResponse>.None;
    }

    public async Task<long> GetAbsoluteMaximumUploadSizeAsync()
    {
        return await GetTransferSettingsAsync()
            .MatchAsync(
                () => 0,
                x => x.MaximumUploadSize);
    }
    
    public async Task<long> GetCurrentMaximumUploadSizeAsync()
    {
        return await GetTransferSettingsAsync()
            .MatchAsync(
                () => 0,
                x => Math.Min(x.MaximumUploadSize, Math.Min(x.AvailableUserSpace, x.AvailableFreeTransferSpace)));
    }

    public async Task<bool> IsUserQuotaReachedAsync()
    {
        return await GetTransferSettingsAsync()
            .MatchAsync(
                () => true,
                x => x.AvailableUserSpace > 0);
    }
    
    public async Task<bool> IsFreeTransferQuotaReachedAsync()
    {
        return await GetTransferSettingsAsync()
            .MatchAsync(
                () => true,
                x => x.AvailableFreeTransferSpace > 0);
    }
}
