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
using Crypter.Common.Contracts.Features.Setting;
using EasyMonads;
using Microsoft.Extensions.Caching.Memory;

namespace Crypter.Common.Client.Services;

public class SettingService : ISettingService
{
    private readonly IUserSessionService _userSessionService;
    private readonly ICrypterApiClient _crypterApiClient;
    
    private readonly IMemoryCache _memoryCache;
    
    public SettingService(IUserSessionService userSessionService, ICrypterApiClient crypterApiClient, IMemoryCache memoryCache)
    {
        _userSessionService = userSessionService;
        _crypterApiClient = crypterApiClient;
        _memoryCache = memoryCache;
    }
    
    public async Task<Maybe<UploadSettings>> GetUploadSettingsAsync()
    {
        const string cacheKey = $"{nameof(SettingService)}:{nameof(GetUploadSettingsAsync)}";
        return await _memoryCache.GetOrCreateAsync<UploadSettings?>(cacheKey, async entry =>
        {
            bool isLoggedIn = await _userSessionService.IsLoggedInAsync();
            Maybe<UploadSettings> uploadSettings = await _crypterApiClient.Setting.GetUploadSettingsAsync(isLoggedIn);
            return uploadSettings
                .IfNone(() => entry.SetValue(null))
                .Bind<UploadSettings?>(x => x)
                .SomeOrDefault(null);
        }, new MemoryCacheEntryOptions{AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)}) ?? Maybe<UploadSettings>.None;
    }
}
