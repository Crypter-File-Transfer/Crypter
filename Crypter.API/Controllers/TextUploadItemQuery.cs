using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;
using CrypterAPI.Models;


namespace CrypterAPI.Controllers
{

   public class TextUploadItemQuery
   {
      public CrypterDB Db { get; }
      public TextUploadItemQuery(CrypterDB db)
      {
         Db = db;
      }

      public async Task<TextUploadItem> FindOneAsync(int id)
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"SELECT `Id`, `UntrustedName`, `UserID`, `Size`, `TimeStamp`, `CharCount`, `Message` FROM `TextUploadItems` WHERE `Id` = @id";
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@id",
            DbType = DbType.Int32,
            Value = id,
         });
         var result = await ReadAllAsync(await cmd.ExecuteReaderAsync());
         return result.Count > 0 ? result[0] : null;
      }

      public async Task<List<TextUploadItem>> LatestItemsAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"SELECT `Id`, `UntrustedName`, `UserID`, `Size`, `TimeStamp`, `CharCount`, `Message` FROM `TextUploadItems` ORDER BY `Id` DESC LIMIT 10;";
         return await ReadAllAsync(await cmd.ExecuteReaderAsync());
      }

      public async Task DeleteAllAsync()
      {
         using var txn = await Db.Connection.BeginTransactionAsync();
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"DELETE FROM `TextUploadItems`";
         //added per https://fl.vu/mysql-trans
         cmd.Transaction = txn;
         await cmd.ExecuteNonQueryAsync();
         await txn.CommitAsync();
      }

      private async Task<List<TextUploadItem>> ReadAllAsync(DbDataReader reader)
      {
         var items = new List<TextUploadItem>();
         using (reader)
         {
            while (await reader.ReadAsync())
            {
               var item = new TextUploadItem(Db)
               {
                  Id = reader.GetInt32(0),
                  UntrustedName = reader.GetString(1),
                  UserID = reader.GetString(2),
                  Size = reader.GetFloat(3),
                  TimeStamp = reader.GetDateTime(4),
                  CharCount = reader.GetString(5),
                  Message = reader.GetString(6)
               };
               items.Add(item);
            }
         }
         return items;
      }
   }
}