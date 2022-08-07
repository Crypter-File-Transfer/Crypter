/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.ClientServices.DeviceStorage.Enums;
using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Repositories;
using Crypter.ClientServices.Services;
using Crypter.ClientServices.Transfer;
using Crypter.ClientServices.Transfer.Models;
using Crypter.Web;
using Crypter.Web.Repositories;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

var builder = WebAssemblyHostBuilder.CreateDefault();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<IClientApiSettings>(builder.Configuration
   .GetSection("ApiSettings")
   .Get<ClientApiSettings>());

builder.Services.AddSingleton(builder.Configuration
   .GetSection("FileTransferSettings")
   .Get<FileTransferSettings>());

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
   .AddSingleton<IUserKeysService, UserKeysService>()
   .AddSingleton<ICompressionService, CompressionService>()
   .AddSingleton<IUserContactsService, UserContactsService>()
   .AddSingleton<IBrowserDownloadFileService, BrowserDownloadFileService>()
   .AddSingleton<IUserPasswordService, UserPasswordService>()
   .AddSingleton<TransferHandlerFactory>()
   .AddSingleton<Func<ICrypterApiService>>(sp => () => sp.GetService<ICrypterApiService>());

var host = builder.Build();

// These constructors will register events and event handlers
host.Services.GetRequiredService<IUserContactsService>();
host.Services.GetRequiredService<IUserKeysService>();
host.Services.GetRequiredService<IUserSessionService>();

await host.RunAsync();
