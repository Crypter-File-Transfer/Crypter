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

using System;
using System.Data;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Crypter.Common.Client.HttpClients;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Repositories;
using Crypter.Core;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Crypter.Test
{
   [SetUpFixture]
   internal class AssemblySetup
   {
      public static string CrypterConnectionString;
      public static string HangfireConnectionString;

      public static string FileStorageLocation;
      
      private IContainer _postgresContainer;
      
      private static Respawner _crypterRespawner;
      private static NpgsqlConnection _crypterConnection;

      private static Respawner _hangfireRespawner;
      private static NpgsqlConnection _hangfireConnection;
      
      [OneTimeSetUp]
      public async Task SetupFixtureAsync()
      {
         ContainerSettings containerSettings = GetContainerSettings();
         
         _postgresContainer = new PostgreSqlBuilder()
            .WithImage(containerSettings.Image)
            .WithPassword(containerSettings.SuperPassword)
            .WithPortBinding(containerSettings.ContainerPort, true)
            .WithBindMount(GetPostgresInitVolume(), "/docker-entrypoint-initdb.d")
            .WithEnvironment("POSTGRES_C_PASSWORD", containerSettings.CrypterUserPassword)
            .WithEnvironment("POSTGRES_HF_PASSWORD", containerSettings.HangfireUserPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready -h 'localhost' -p '5432'"))
            .Build();

         await _postgresContainer.StartAsync();

         NpgsqlConnectionStringBuilder crypterConnectionStringBuilder = new NpgsqlConnectionStringBuilder
         {
            Host = _postgresContainer.Hostname,
            Port = _postgresContainer.GetMappedPublicPort(containerSettings.ContainerPort),
            Database = containerSettings.CrypterDatabaseName,
            Username = containerSettings.CrypterUserName,
            Password = containerSettings.CrypterUserPassword
         };

         CrypterConnectionString = crypterConnectionStringBuilder.ConnectionString;

         NpgsqlConnectionStringBuilder hangfireConnectionStringBuilder = new NpgsqlConnectionStringBuilder
         {
            Host = _postgresContainer.Hostname,
            Port = _postgresContainer.GetMappedPublicPort(containerSettings.ContainerPort),
            Database = containerSettings.HangfireDatabaseName,
            Username = containerSettings.HangfireUserName,
            Password = containerSettings.HangfireUserPassword
         };

         HangfireConnectionString = hangfireConnectionStringBuilder.ConnectionString;
         
         string osName = OperatingSystem.IsWindows()
            ? "Windows"
            : OperatingSystem.IsLinux()
               ? "Linux"
               : throw new NotImplementedException("Operating system not implemented.");

         FileStorageLocation = AssemblySetup.GetTestSettings()
            .GetSection($"IntegrationTestingOnly:TransferStorageLocation:{osName}")
            .Get<string>();
      }

      [OneTimeTearDown]
      public async Task TeardownFixtureAsync()
      {
         if (_postgresContainer is not null)
         {
            await _postgresContainer.StopAsync();
         }
      }

      internal static async Task<WebApplicationFactory<Program>> CreateWebApplicationFactoryAsync(IServiceCollection overrides = null)
      {
         WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
               builder
                  .UseConfiguration(GetTestSettings())
                  .UseEnvironment("Test")
                  .ConfigureServices(services =>
                  {
                     if (overrides is null)
                     {
                        return;
                     }

                     foreach (ServiceDescriptor overrideService in overrides)
                     {
                        ReplaceService(services, overrideService);
                     }
                  });

               builder.UseSetting("ConnectionStrings:DefaultConnection", CrypterConnectionString);
               builder.UseSetting("ConnectionStrings:HangfireConnection", HangfireConnectionString);
               builder.UseSetting("TransferStorageSettings:Location", FileStorageLocation);
            });

         using IServiceScope scope = factory.Services.CreateScope();
         await using DataContext dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
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

      internal static async Task InitializeRespawnerAsync()
      {
         _crypterConnection = new NpgsqlConnection(CrypterConnectionString);
         _hangfireConnection = new NpgsqlConnection(HangfireConnectionString);

         RespawnerOptions respawnOptions = new RespawnerOptions
         {
            DbAdapter = DbAdapter.Postgres,
            WithReseed = true,
            TablesToIgnore = new Table[]
            {
               "schema"
            }
         };

         _crypterRespawner = await InitializeRespawnerAsync(_crypterConnection, respawnOptions);
         _hangfireRespawner = await InitializeRespawnerAsync(_hangfireConnection, respawnOptions);
      }
      
      private static async Task<Respawner> InitializeRespawnerAsync(NpgsqlConnection connection, RespawnerOptions options)
      {
         connection.Open();
         Respawner respawner = await Respawner.CreateAsync(connection, options);
         connection.Close();

         return respawner;
      }
      
      internal static IConfiguration GetTestSettings()
      {
         DirectoryInfo repoDirectory = GetRepoPath();

         string testSettings = Path.Join(repoDirectory.FullName, "Crypter.Test", "appsettings.Test.json");
         if (!Path.Exists(testSettings))
         {
            throw new FileNotFoundException("Failed to find ./Crypter.Test/appsettings.Test.json.");
         }

         return new ConfigurationBuilder()
            .AddJsonFile(testSettings)
            .Build();
      }
      
      private static DirectoryInfo GetRepoPath()
      {
         string assemblyLocation = Assembly.GetExecutingAssembly().Location;
         
         DirectoryInfo directory = new DirectoryInfo(assemblyLocation);
         do
         {
            directory = directory.Parent;
         } while (directory.Name != "Crypter.Test");

         return directory.Parent;
      }
      
      private static string GetPostgresInitVolume()
      {
         DirectoryInfo repoDirectory = GetRepoPath();

         string postgresInitVolume = Path.Join(repoDirectory.FullName, "Volumes", "PostgreSQL", "postgres-init-files");
         if (!Path.Exists(postgresInitVolume))
         {
            throw new FileNotFoundException("Failed to find the ./Volumes/PostgreSQL/postgres-init-files directory.");
         }

         return postgresInitVolume;
      }

      private static ContainerSettings GetContainerSettings()
      {
         return GetTestSettings()
            .GetSection("IntegrationTestingOnly:PostgresContainer")
            .Get<ContainerSettings>();
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
               await connection.CloseAsync();
            }
         }
      }

      internal static async Task ResetServerDataAsync()
      {
         try
         {
            Directory.Delete(FileStorageLocation, true);
         }
         catch (Exception)
         { }

         await ResetDatabaseAsync(_crypterRespawner, _crypterConnection);
         await ResetDatabaseAsync(_hangfireRespawner, _hangfireConnection);
      }
      
      private class ContainerSettings
      {
         public string Image { get; init; }
         public int ContainerPort { get; init; }
         public string SuperPassword { get; init; }
         public string CrypterDatabaseName { get; init; }
         public string CrypterUserName { get; init; }
         public string CrypterUserPassword { get; init; }
         public string HangfireDatabaseName { get; init; }
         public string HangfireUserName { get; init; }
         public string HangfireUserPassword { get; init; }
      }
   }
}