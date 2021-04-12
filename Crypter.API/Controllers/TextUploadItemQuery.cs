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

      public async Task<TextUploadItem> FindOneAsync(string id)
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"SELECT `Id`,`UserID`,`UntrustedName`, `Size`, `Signature`, `Created`, `ExpirationDate`, `EncryptedMessagePath` FROM `MessageUploads` WHERE `Id` = @id";
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@id",
            DbType = DbType.String,
            Value = id,
         });
         var result = await ReadAllAsync(await cmd.ExecuteReaderAsync());
         return result.Count > 0 ? result[0] : null;
      }

      public async Task<List<TextUploadItem>> LatestItemsAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"SELECT `Id`, `UserID`, `UntrustedName`, `Size`,`Signature`,`Created`, `ExpirationDate`, `EncryptedMessagePath` FROM `MessageUploads` ORDER BY `Id` DESC LIMIT 10;";
         return await ReadAllAsync(await cmd.ExecuteReaderAsync());
      }

      public async Task DeleteAllAsync()
      {
         using var txn = await Db.Connection.BeginTransactionAsync();
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"DELETE FROM `MessageUplaods`";
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
                  Id = reader.GetString(0),
                  UserID = reader.GetString(1),
                  UntrustedName = reader.GetString(2),
                  Size = reader.GetInt16(3),
                  Signature = reader.GetString(4),
                  Created = reader.GetDateTime(5),
                  ExpirationDate = reader.GetDateTime(6),
                  EncryptedMessagePath = reader.GetString(7)
               };
               items.Add(item);
            }
         }
         return items;
      }
   }
}