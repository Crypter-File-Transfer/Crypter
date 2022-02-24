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

using Crypter.ClientServices.Implementations;
using Crypter.ClientServices.Interfaces;
using Crypter.CryptoLib.Services;
using Crypter.Web;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<ClientAppSettings>(sp =>
{
   var config = sp.GetService<IConfiguration>();
   return config.Get<ClientAppSettings>();
});

builder.Services.AddSingleton<IClientApiSettings>(sp =>
{
   var config = sp.GetService<IConfiguration>();
   return config.GetSection("ApiSettings").Get<ClientApiSettings>();
});

builder.Services
   .AddBlazorDownloadFile()
   .AddScoped(sp => new HttpClient())
   .AddScoped<IAuthenticationService, AuthenticationService>()
   .AddScoped<IHttpService, HttpService>()
   .AddScoped<ITokenRepository, TokenRepository>()
   .AddScoped<IDeviceStorageService<BrowserStoredObjectType, BrowserStorageLocation>, BrowserStorageService>()
   .AddScoped<ICrypterApiService, CrypterApiService>()
   .AddScoped<IUserKeysService, UserKeysService>()
   .AddScoped<ISimpleEncryptionService, SimpleEncryptionService>();

await builder.Build().RunAsync();
