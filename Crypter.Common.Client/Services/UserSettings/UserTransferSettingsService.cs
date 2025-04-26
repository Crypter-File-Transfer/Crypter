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
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Contracts.Features.UserSettings.TransferSettings;
using EasyMonads;
using Microsoft.Extensions.Caching.Memory;

namespace Crypter.Common.Client.Services.UserSettings;

public class UserTransferSettingsService : IUserTransferSettingsService, IDisposable
{
    private readonly IUserSessionService _userSessionService;
    private readonly ICrypterApiClient _crypterApiClient;
    
    private readonly IMemoryCache _memoryCache;
    private readonly SemaphoreSlim _memoryCacheLock = new SemaphoreSlim(1, 1);
    
    private const string TransferSettingsCacheKey = $"{nameof(UserTransferSettingsService)}:TransferSettings";
    
    public UserTransferSettingsService(IUserSessionService userSessionService, ICrypterApiClient crypterApiClient, IMemoryCache memoryCache)
    {
        _userSessionService = userSessionService;
        _crypterApiClient = crypterApiClient;
        _memoryCache = memoryCache;

        userSessionService.UserLoggedInEventHandler += RecycleAsync;
        userSessionService.UserLoggedOutEventHandler += RecycleAsync;
    }
    
    public async Task<Maybe<GetTransferSettingsResponse>> GetTransferSettingsAsync()
    {
        try
        {
            await _memoryCacheLock.WaitAsync();
            return await _memoryCache.GetOrCreateAsync<GetTransferSettingsResponse?>(TransferSettingsCacheKey, async entry =>
            {
                bool isLoggedIn = await _userSessionService.IsLoggedInAsync();
                Maybe<GetTransferSettingsResponse> uploadSettings = await _crypterApiClient.UserSetting.GetTransferSettingsAsync(isLoggedIn);
                return uploadSettings
                    .IfNone(() => entry.SetValue(null))
                    .Bind<GetTransferSettingsResponse?>(x => x)
                    .SomeOrDefault(null);
            }, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) }) ?? Maybe<GetTransferSettingsResponse>.None;
        }
        finally
        {
            _memoryCacheLock.Release();
        }
    }

    public async Task<long> GetAbsoluteMaximumUploadSizeAsync()
    {
        return await GetTransferSettingsAsync()
            .Select(x => x.MaximumUploadSize)
            .SomeOrDefaultAsync(0);
    }

    public async Task<int> GetAbsoluteMaximumMessageLengthAsync()
    {
        return await GetTransferSettingsAsync()
            .Select(x => x.MaximumMessageLength)
            .SomeOrDefaultAsync(0);
    }
    
    public async Task<long> GetCurrentMaximumUploadSizeAsync()
    {
        return await GetTransferSettingsAsync()
            .Select(x => Math.Min(x.MaximumUploadSize, Math.Min(x.AvailableUserSpace, x.AvailableFreeTransferSpace)))
            .SomeOrDefaultAsync(0);
    }

    public async Task<bool> IsUserQuotaReachedAsync()
    {
        return await GetTransferSettingsAsync()
            .Select(x => x.AvailableUserSpace == 0)
            .SomeOrDefaultAsync(true);
    }
    
    public async Task<bool> IsFreeTransferQuotaReachedAsync()
    {
        return await GetTransferSettingsAsync()
            .Select(x => x.AvailableFreeTransferSpace == 0)
            .SomeOrDefaultAsync(true);
    }
    
    private async void RecycleAsync(object? _, EventArgs __)
    {
        try
        {
            await _memoryCacheLock.WaitAsync();
            _memoryCache.Remove(TransferSettingsCacheKey);
        }
        finally
        {
            _memoryCacheLock.Release();
        }
    }

    public void Dispose()
    {
        _userSessionService.UserLoggedInEventHandler -= RecycleAsync;
        _userSessionService.UserLoggedOutEventHandler -= RecycleAsync;
        GC.SuppressFinalize(this);
    }
}
