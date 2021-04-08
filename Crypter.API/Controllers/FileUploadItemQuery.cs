using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;
using CrypterAPI.Models;

namespace CrypterAPI.Controllers
{
   public class FileUploadItemQuery
   {
      public CrypterDB Db { get; }
      public FileUploadItemQuery(CrypterDB db)
      {
         Db = db;
      }

      public async Task<FileUploadItem> FindOneAsync(int id)
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"SELECT `Id`, `UntrustedName`, `UserID`, `Size`, `TimeStamp`,`FileContent` FROM `FileUploadItems` WHERE `Id` = @id";
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@id",
            DbType = DbType.Int32,
            Value = id,
         });
         var result = await ReadAllAsync(await cmd.ExecuteReaderAsync());
         return result.Count > 0 ? result[0] : null;
      }

      public async Task<List<FileUploadItem>> LatestItemsAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"SELECT `Id`, `UntrustedName`, `UserID`, `Size`, `TimeStamp`, `FileContent` FROM `FileUploadItems` ORDER BY `Id` DESC LIMIT 10;";
         return await ReadAllAsync(await cmd.ExecuteReaderAsync());
      }

      public async Task DeleteAllAsync()
      {
         using var txn = await Db.Connection.BeginTransactionAsync();
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"DELETE FROM `FileUploadItems`";
         //added per https://fl.vu/mysql-trans
         cmd.Transaction = txn;
         await cmd.ExecuteNonQueryAsync();
         await txn.CommitAsync();
      }

      private async Task<List<FileUploadItem>> ReadAllAsync(DbDataReader reader)
      {
         var items = new List<FileUploadItem>();
         using (reader)
         {
            while (await reader.ReadAsync())
            {
               var item = new FileUploadItem(Db)
               {
                  Id = reader.GetInt32(0),
                  UntrustedName = reader.GetString(1),
                  UserID = reader.GetString(2),
                  Size = reader.GetFloat(3),
                  TimeStamp = reader.GetDateTime(4),
                  FileContent = reader.GetString(5)
               };
               items.Add(item);
            }
         }
         return items;
      }
   }
}