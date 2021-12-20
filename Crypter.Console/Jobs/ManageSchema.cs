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

using Npgsql;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.Console.Jobs
{
   internal class ManageSchema {

      private readonly string ConnectionString;

      public ManageSchema(string connectionString)
      {
         ConnectionString = connectionString;
      }

      public async Task CreateSchemaAsync()
      {
         await using var connection = new NpgsqlConnection(ConnectionString);
         await connection.OpenAsync();

         try
         {
            List<string> scripts = new()
            {
               await GetTableCreationScriptAsync("Create_FileTransfer.sql"),
               await GetTableCreationScriptAsync("Create_MessageTransfer.sql"),
               await GetTableCreationScriptAsync("Create_User.sql"),
               await GetTableCreationScriptAsync("Create_UserEd25519KeyPair.sql"),
               await GetTableCreationScriptAsync("Create_UserEmailVerification.sql"),
               await GetTableCreationScriptAsync("Create_UserNotificationSetting.sql"),
               await GetTableCreationScriptAsync("Create_UserPrivacySetting.sql"),
               await GetTableCreationScriptAsync("Create_UserProfile.sql"),
               await GetTableCreationScriptAsync("Create_UserX25519KeyPair.sql"),
               await GetTableCreationScriptAsync("Create_Schema.sql"),
               await GetTableCreationScriptAsync("Create_UserToken.sql")
            };

            scripts.ForEach(x => ExecuteSqlScriptNonQuery(connection, x));
         }
         catch (System.Exception e)
         {
            System.Console.WriteLine(e.Message);
            throw;
         }
         finally
         {
            connection.Close();
         }
      }

      public async Task DeleteSchemaAsync()
      {
         await using var connection = new NpgsqlConnection(ConnectionString);
         await connection.OpenAsync();

         try
         {
            List<string> scripts = new()
            {
               await GetTableDropScriptAsync("Drop_UserToken.sql"),
               await GetTableDropScriptAsync("Drop_FileTransfer.sql"),
               await GetTableDropScriptAsync("Drop_MessageTransfer.sql"),
               await GetTableDropScriptAsync("Drop_UserEd25519KeyPair.sql"),
               await GetTableDropScriptAsync("Drop_UserEmailVerification.sql"),
               await GetTableDropScriptAsync("Drop_UserNotificationSetting.sql"),
               await GetTableDropScriptAsync("Drop_UserPrivacySetting.sql"),
               await GetTableDropScriptAsync("Drop_UserProfile.sql"),
               await GetTableDropScriptAsync("Drop_UserX25519KeyPair.sql"),
               await GetTableDropScriptAsync("Drop_User.sql"),
               await GetTableDropScriptAsync("Drop_Schema.sql")
            };

            scripts.ForEach(x => ExecuteSqlScriptNonQuery(connection, x));
         }
         catch (System.Exception e)
         {
            System.Console.WriteLine(e.Message);
            throw;
         }
         finally
         {
            connection.Close();
         }
      }
      
      /// <summary>
      /// Perform a migration of the Crypter database.
      /// </summary>
      /// <returns></returns>
      public async Task PerformMigration(string filename)
      {
         await using var connection = new NpgsqlConnection(ConnectionString);
         await connection.OpenAsync();

         try
         {
            var migration = await GetMigrationScriptAsync(filename);
            ExecuteSqlScriptNonQuery(connection, migration);
         }
         catch (System.Exception e)
         {
            System.Console.WriteLine(e.Message);
            throw;
         }
         finally
         {
            connection.Close();
         }
      }

      private static async Task<string> GetTableCreationScriptAsync(string scriptFilename)
      {
         var pathSegments = new string[]
         {
            "SqlScripts",
            "Crypter",
            "Create",
            scriptFilename
         };
         var filePath = Path.Join(pathSegments);
         return await File.ReadAllTextAsync(filePath);
      }

      private static async Task<string> GetTableDropScriptAsync(string scriptFilename)
      {
         var pathSegments = new string[]
         {
            "SqlScripts",
            "Crypter",
            "Drop",
            scriptFilename
         };
         var filePath = Path.Join(pathSegments);
         return await File.ReadAllTextAsync(filePath);
      }

      private static async Task<string> GetMigrationScriptAsync(string scriptFilename)
      {
         var pathSegments = new string[]
         {
            "SqlScripts",
            "Crypter",
            "Migrations",
            scriptFilename
         };
         var filePath = Path.Join(pathSegments);
         return await File.ReadAllTextAsync(filePath);
      }

      private static void ExecuteSqlScriptNonQuery(NpgsqlConnection openConnection, string sql)
      {
         using var cmd = new NpgsqlCommand(sql, openConnection);
         cmd.ExecuteNonQuery();
      }
   }
}
