/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Web.Services;
using Crypter.Web.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Crypter.Web.Services.API;
using Crypter.CryptoLib.Services;

namespace Crypter.Web
{
   public class Program
   {
      public static async Task Main(string[] args)
      {
         var builder = WebAssemblyHostBuilder.CreateDefault(args);
         builder.RootComponents.Add<App>("#app");

         builder.Services.AddScoped(_ =>
         {
            return LoadAppSettings("Crypter.Web.appsettings.json")
                           .Get<AppSettings>();
         });

         builder.Services
            .AddBlazorDownloadFile()
            .AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
            .AddScoped<IAuthenticationService, AuthenticationService>()
            .AddScoped<Func<IAuthenticationService>>(cont => () => cont.GetService<IAuthenticationService>())
            .AddScoped<IHttpService, HttpService>()
            .AddScoped<ILocalStorageService, LocalStorageService>()
            .AddScoped<ITransferApiService, TransferApiService>()
            .AddScoped<IUserApiService, UserApiService>()
            .AddScoped<IAuthenticationApiService, AuthenticationApiService>()
            .AddScoped<IMetricsApiService, MetricsApiService>()
            .AddScoped<IUserKeysService, UserKeysService>()
            .AddScoped<ISimpleEncryptionService, SimpleEncryptionService>();

         var host = builder.Build();

         var authenticationService = host.Services.GetRequiredService<IAuthenticationService>();

         await host.RunAsync();
      }

      public static IConfigurationRoot LoadAppSettings(string filename)
      {
         var stream = Assembly.GetExecutingAssembly()
                              .GetManifestResourceStream(filename);

         return new ConfigurationBuilder()
                 .AddJsonStream(stream)
                 .Build();
      }
   }
}
