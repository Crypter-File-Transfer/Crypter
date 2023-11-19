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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using NUnit.Framework;

namespace Crypter.Test.Container_Tests;

[TestFixture]
internal class PostgresContainer_Tests
{
   private WebApplicationFactory<Program> _factory;

   [SetUp]
   public async Task SetupTestAsync()
   {
      _factory = await AssemblySetup.CreateWebApplicationFactoryAsync();
   }
      
   [TearDown]
   public async Task TeardownTestAsync()
   {
      await _factory.DisposeAsync();
   }

   [Test]
   public void Crypter_Database_Connection_Works()
   {
      using NpgsqlConnection connection = new NpgsqlConnection(AssemblySetup.CrypterConnectionString);
      Assert.DoesNotThrowAsync(() => connection.OpenAsync());
      Assert.DoesNotThrowAsync(() => connection.CloseAsync());
   }

   [Test]
   public void Hangfire_Database_Connection_Works()
   {
      using NpgsqlConnection connection = new NpgsqlConnection(AssemblySetup.HangfireConnectionString);
      Assert.DoesNotThrowAsync(() => connection.OpenAsync());
      Assert.DoesNotThrowAsync(() => connection.CloseAsync());
   }

   [Test]
   public async Task Crypter_User_Cannot_Connect_To_Hangfire_Database()
   {
      PostgresContainerSettings postgresSettings = ContainerService.GetPostgresContainerSettings();

      using NpgsqlConnection connection = new NpgsqlConnection(AssemblySetup.CrypterConnectionString);
      await connection.OpenAsync();

      const string query = "SELECT has_database_privilege(@Username, @DatabaseName, 'CONNECT');";
      var parameters = new
      {
         Username = postgresSettings.CrypterUserName,
         DatabaseName = postgresSettings.HangfireDatabaseName
      };
         
      IEnumerable<bool> results = await connection.QueryAsync<bool>(query, parameters);
      bool canConnect = results.First();

      Assert.False(canConnect);
         
      await connection.CloseAsync();
   }

   [Test]
   public async Task Hangfire_User_Cannot_Connect_To_Crypter_Database()
   {
      PostgresContainerSettings postgresSettings = ContainerService.GetPostgresContainerSettings();
         
      using NpgsqlConnection connection = new NpgsqlConnection(AssemblySetup.HangfireConnectionString);
      await connection.OpenAsync();
         
      const string query = "SELECT has_database_privilege(@Username, @DatabaseName, 'CONNECT');";
      var parameters = new
      {
         Username = postgresSettings.HangfireUserName,
         DatabaseName = postgresSettings.CrypterDatabaseName
      };
         
      IEnumerable<bool> results = await connection.QueryAsync<bool>(query, parameters);
      bool canConnect = results.First();

      Assert.False(canConnect);
         
      await connection.CloseAsync();
   }
}