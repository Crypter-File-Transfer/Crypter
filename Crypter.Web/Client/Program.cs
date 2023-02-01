/*
 * Copyright (C) 2023 Crypter File Transfer
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

using BlazorSodium.Extensions;
using Crypter.Common.Client.DeviceStorage.Enums;
using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Implementations;
using Crypter.Common.Client.Transfer;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Crypto.Common;
using Crypter.Crypto.Providers.Browser;
using Crypter.Web;
using Crypter.Web.Models.Settings;
using Crypter.Web.Repositories;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(sp =>
{
   var config = sp.GetService<IConfiguration>();
   return config.Get<ClientSettings>();
});

builder.Services.AddSingleton<IClientApiSettings>(sp =>
{
   var config = sp.GetService<IConfiguration>();
   return config.GetSection("ApiSettings").Get<ClientApiSettings>();
});

builder.Services.AddSingleton(sp =>
{
   var config = sp.GetService<IConfiguration>();
   return config.GetSection("TransferSettings").Get<TransferSettings>();
});

builder.Services.AddHttpClient<ICrypterHttpService, CrypterHttpService>(httpClient =>
{
   var config = builder.Services
      .BuildServiceProvider()
      .GetService<IClientApiSettings>();

   httpClient.BaseAddress = new Uri(config.ApiBaseUrl);
});

builder.Services.AddHttpClient<ICrypterAuthenticatedHttpService, CrypterAuthenticatedHttpService>(httpClient =>
{
   var config = builder.Services
      .BuildServiceProvider()
      .GetService<IClientApiSettings>();

   httpClient.BaseAddress = new Uri(config.ApiBaseUrl);
});

builder.Services
   .AddSingleton<IDeviceRepository<BrowserStorageLocation>, BrowserRepository>()
   .AddSingleton<ITokenRepository, BrowserTokenRepository>()
   .AddSingleton<IUserKeysRepository, BrowserUserKeysRepository>()
   .AddSingleton<IUserSessionRepository, BrowserUserSessionRepository>()
   .AddSingleton<IUserSessionService, UserSessionService<BrowserStorageLocation>>()
   .AddSingleton<ICrypterApiService, CrypterApiService>()
   .AddSingleton<IUserContactsService, UserContactsService>()
   .AddSingleton<IUserPasswordService, UserPasswordService>()
   .AddSingleton<IUserKeysService, UserKeysService>()
   .AddSingleton<TransferHandlerFactory>()
   .AddSingleton<Func<ICrypterApiService>>(sp => () => sp.GetService<ICrypterApiService>());

if (OperatingSystem.IsBrowser())
{
   builder.Services.AddBlazorSodium();
   builder.Services.AddSingleton<ICryptoProvider, BrowserCryptoProvider>();
}

WebAssemblyHost host = builder.Build();

// Resolve services so they can initialize
IUserContactsService contactsService = host.Services.GetRequiredService<IUserContactsService>();
IUserSessionService userSessionService = host.Services.GetRequiredService<IUserSessionService>();

await host.RunAsync();
