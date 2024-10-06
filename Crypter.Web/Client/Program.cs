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
using BlazorSodium.Extensions;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.HttpClients;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Interfaces.Services.UserSettings;
using Crypter.Common.Client.Models;
using Crypter.Common.Client.Services;
using Crypter.Common.Client.Services.UserSettings;
using Crypter.Common.Client.Transfer;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Exceptions;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Browser;
using Crypter.Web;
using Crypter.Web.Models.Settings;
using Crypter.Web.Repositories;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

Console.WriteLine($"Environment: {builder.HostEnvironment.Environment}");

builder.Services.AddSingleton(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    return config.Get<ClientSettings>()
           ?? throw new ConfigurationException("Failed to load client settings.");
});

builder.Services.AddSingleton(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    return config.GetSection("ApiSettings").Get<ClientApiSettings>()
        ?? throw new ConfigurationException("Failed to load ApiSettings.");
});

builder.Services.AddSingleton(sp =>
{
    IConfiguration config = sp.GetRequiredService<IConfiguration>();
    return config.GetSection("ClientTransferSettings").Get<ClientTransferSettings>()
        ?? throw new ConfigurationException("Failed to load ClientTransferSettings.");
});

builder.Services.AddTransient<BrowserHttpMessageHandler>();
builder.Services.AddHttpClient<ICrypterApiClient, CrypterApiClient>(httpClient =>
{
    ClientApiSettings config = builder.Services
        .BuildServiceProvider()
        .GetRequiredService<ClientApiSettings>();

    httpClient.BaseAddress = new Uri(config.ApiBaseUrl);
    httpClient.Timeout = Timeout.InfiniteTimeSpan;
}).AddHttpMessageHandler<BrowserHttpMessageHandler>();

builder.Services
    .AddSingleton<IDeviceRepository<BrowserStorageLocation>, BrowserRepository>()
    .AddSingleton<ITokenRepository, BrowserTokenRepository>()
    .AddSingleton<IUserKeysRepository, BrowserUserKeysRepository>()
    .AddSingleton<IUserSessionRepository, BrowserUserSessionRepository>()
    .AddSingleton<IUserSessionService, UserSessionService<BrowserStorageLocation>>()
    .AddSingleton<IUserContactsService, UserContactsService>()
    .AddSingleton<IUserPasswordService, UserPasswordService>()
    .AddSingleton<IUserRecoveryService, UserRecoveryService>()
    .AddSingleton<IUserKeysService, UserKeysService>()
    .AddSingleton<IUserProfileSettingsService, UserProfileSettingsService>()
    .AddSingleton<IUserContactInfoSettingsService, UserContactInfoSettingsService>()
    .AddSingleton<IUserNotificationSettingsService, UserNotificationSettingsService>()
    .AddSingleton<IUserPrivacySettingsService, UserPrivacySettingsService>()
    .AddSingleton<TransferHandlerFactory>()
    .AddSingleton<Func<ICrypterApiClient>>(sp => sp.GetRequiredService<ICrypterApiClient>);

if (OperatingSystem.IsBrowser())
{
    builder.Services.AddBlazorSodium();
    builder.Services
        .AddSingleton<ICryptoProvider, BrowserCryptoProvider>()
        .AddSingleton<IFileSaverService, FileSaverService>();
}

WebAssemblyHost host = builder.Build();

// Resolve services so they can subscribe to events
IUserContactsService _ = host.Services.GetRequiredService<IUserContactsService>();
IUserSessionService __ = host.Services.GetRequiredService<IUserSessionService>();

await host.RunAsync();
