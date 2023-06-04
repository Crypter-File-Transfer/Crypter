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

using Crypter.Common.Client.HttpClients;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Repositories;
using Crypter.Core;
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

      private Respawner _crypterRespawner;
      private NpgsqlConnection _defaultConnection;

      private Respawner _hangfireRespawner;
      private NpgsqlConnection _hangfireConnection;

      public const string TestEnvironmentName = "Test";

      internal Setup()
      {
         IConfigurationRoot configuration = GetIntegrationConfiguration();

         DefaultConnectionString = configuration.GetConnectionString("DefaultConnection");
         HangfireConnectionString = configuration.GetConnectionString("HangfireConnection");
         FileStorageLocation = configuration.GetSection("TransferStorageSettings")
            .Get<TransferStorageSettings>().Location;
      }

      internal async Task InitializeRespawnerAsync()
      {
         _defaultConnection = new NpgsqlConnection(DefaultConnectionString);
         _hangfireConnection = new NpgsqlConnection(HangfireConnectionString);

         RespawnerOptions respawnOptions = new RespawnerOptions
         {
            DbAdapter = DbAdapter.Postgres,
            WithReseed = true
         };

         _crypterRespawner = await InitializeRespawnerAsync(_defaultConnection, respawnOptions);
         _hangfireRespawner = await InitializeRespawnerAsync(_defaultConnection, respawnOptions);
      }

      internal async Task ResetServerDataAsync()
      {
         try
         {
            Directory.Delete(FileStorageLocation, true);
         }
         catch (Exception)
         { }

         await ResetDatabaseAsync(_crypterRespawner, _defaultConnection);
         await ResetDatabaseAsync(_hangfireRespawner, _hangfireConnection);
      }

      internal static async Task<WebApplicationFactory<Program>> SetupWebApplicationFactoryAsync(IServiceCollection overrides = null)
      {
         WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
         {
            builder
               .UseEnvironment(TestEnvironmentName)
               .UseConfiguration(GetIntegrationConfiguration())
               .ConfigureServices(services =>
               {
                  if (overrides is not null)
                  {
                     foreach (var overrideService in overrides)
                     {
                        ReplaceService(services, overrideService);
                     }
                  }
               });
         });

         DataContext dataContext = factory.Services.GetService<DataContext>();
         await dataContext.Database.EnsureCreatedAsync();

         return factory;
      }

      private static void ReplaceService(IServiceCollection serviceCollection, ServiceDescriptor service)
      {
         if (service is not null)
         {
            serviceCollection.Remove(service);
         }

         serviceCollection.Add(service);
      }

      internal static (ICrypterApiClient crypterApiClient, ITokenRepository tokenRepository) SetupCrypterApiClient(HttpClient webApplicationHttpClient)
      {
         ITokenRepository memoryTokenRepository = new MemoryTokenRepository();
         ICrypterApiClient crypterApiClient = new CrypterApiClient(webApplicationHttpClient, memoryTokenRepository);
         return (crypterApiClient, memoryTokenRepository);
      }

      internal static IConfigurationRoot GetIntegrationConfiguration()
      {
         string operatingSystem = OperatingSystem.IsWindows()
            ? "Windows"
            : OperatingSystem.IsLinux()
               ? "Linux"
               : throw new NotImplementedException("Unit testing for the current operating system has not been implemented.");

         string assemblyLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         string filepath = Path.Combine(assemblyLocation, "Integration_Tests", "Configuration", $"appsettings.{TestEnvironmentName}.{operatingSystem}.json");

         return new ConfigurationBuilder()
            .AddJsonFile(filepath)
            .Build();
      }

      private static async Task<Respawner> InitializeRespawnerAsync(NpgsqlConnection connection, RespawnerOptions options)
      {
         connection.Open();
         Respawner respawner = await Respawner.CreateAsync(connection, options);
         connection.Close();

         return respawner;
      }

      private static async Task ResetDatabaseAsync(Respawner respawner, NpgsqlConnection connection)
      {
         try
         {
            connection.Open();
            await respawner.ResetAsync(connection);
         }
         catch (Exception)
         { }
         finally
         {
            if (connection.State == ConnectionState.Open)
            {
               connection.Close();
            }
         }
      }
   }
}
