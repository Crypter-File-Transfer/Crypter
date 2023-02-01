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

using Crypter.Common.Client.Implementations;
using Crypter.Common.Client.Implementations.Repositories;
using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Core.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Crypter.Test.Integration_Tests.Common
{
   internal class Setup
   {
      private readonly string DefaultConnectionString;
      private readonly string HangfireConnectionString;
      private readonly string FileStorageLocation;

      private NpgsqlConnection _defaultConnection;
      private NpgsqlConnection _hangfireConnection;

      internal Respawner CrypterRespawner { get; private set; }
      internal Respawner HangfireRespawner { get; private set; }

      public Setup()
      {
         IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile(GetConfigurationFile());

         IConfigurationRoot configuration = configurationBuilder.Build();

         DefaultConnectionString = configuration.GetConnectionString("DefaultConnection");
         HangfireConnectionString = configuration.GetConnectionString("HangfireConnection");
         FileStorageLocation = configuration.GetSection("TransferStorageSettings")
            .Get<TransferStorageSettings>().Location;
      }

      public async Task InitializeRespawnerAsync()
      {
         _defaultConnection = new NpgsqlConnection(DefaultConnectionString);
         _hangfireConnection = new NpgsqlConnection(HangfireConnectionString);

         RespawnerOptions respawnOptions = new RespawnerOptions
         {
            DbAdapter = DbAdapter.Postgres
         };

         _defaultConnection.Open();
         CrypterRespawner = await Respawner.CreateAsync(_defaultConnection, respawnOptions);
         _defaultConnection.Close();

         _hangfireConnection.Open();
         HangfireRespawner = await Respawner.CreateAsync(_hangfireConnection, respawnOptions);
         _hangfireConnection.Close();
      }

      public async Task ResetBetweenTestRunsAsync()
      {
         try
         {
            Directory.Delete(FileStorageLocation, true);
         }
         catch (Exception)
         { }
         
         try
         {
            _defaultConnection.Open();
            await CrypterRespawner.ResetAsync(_defaultConnection);
         }
         catch (Exception)
         { }
         finally
         {
            if (_defaultConnection.State == ConnectionState.Open)
            {
               _defaultConnection.Close();
            }
         }

         try
         {
            _hangfireConnection.Open();
            await HangfireRespawner.ResetAsync(_hangfireConnection);
         }
         catch (Exception)
         { }
         finally
         {
            if (_hangfireConnection.State == ConnectionState.Open)
            {
               _hangfireConnection.Close();
            }
         }
      }

      internal static ICrypterApiService SetupCrypterApiService(HttpClient webApplicationHttpClient)
      {
         ServiceCollection serviceCollection = new ServiceCollection();

         serviceCollection
            .AddSingleton<ICrypterHttpService>(sp => new CrypterHttpService(webApplicationHttpClient))
            .AddSingleton<ICrypterAuthenticatedHttpService>(sp => new CrypterAuthenticatedHttpService(
               webApplicationHttpClient,
               sp.GetService<ITokenRepository>(),
               sp.GetService<Func<ICrypterApiService>>()))
            .AddSingleton<ITokenRepository, MemoryTokenRepository>()
            .AddSingleton<ICrypterApiService, CrypterApiService>()
            .AddSingleton<Func<ICrypterApiService>>(sp => () => sp.GetService<ICrypterApiService>());

         ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
         return serviceProvider.GetRequiredService<ICrypterApiService>();
      }

      internal static WebApplicationFactory<Program> SetupWebApplicationFactory()
      {
         return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
         {
            builder
               .UseEnvironment("integration")
               .ConfigureAppConfiguration((context, configuration) =>
               {
                  configuration.AddJsonFile(GetConfigurationFile(), false, false);
                  configuration.AddEnvironmentVariables();
               });
         });
      }

      private static string GetConfigurationFile()
      {
         string assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         return Path.Combine(assemblyLocation, "Integration_Tests", "Configuration", "appsettings.integration.json");
      }
   }
}
