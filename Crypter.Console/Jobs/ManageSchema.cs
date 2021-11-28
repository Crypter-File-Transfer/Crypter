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
               await GetTableCreationScriptAsync("Create_Schema.sql")
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
      /// Perform the first, codified migration of the Crypter database.
      /// </summary>
      /// <returns></returns>
      public async Task PerformInitialMigration()
      {
         await using var connection = new NpgsqlConnection(ConnectionString);
         await connection.OpenAsync();

         try
         {
            var migration = await GetMigrationScriptAsync("Migration1.sql");
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
