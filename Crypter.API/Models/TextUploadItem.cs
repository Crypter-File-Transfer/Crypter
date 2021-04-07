using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
namespace CrypterAPI.Models
{
   //TextUpload inherits from UploadItem
   public class TextUploadItem : UploadItem
   {
      //add additional members/ methods unique to text uploads
      public string CharCount { get; set; }
      public string Message { get; set; }
      //constructor sets TimeStamp upon instantiation
      public TextUploadItem()
      {
         this.TimeStamp = DateTime.UtcNow;
      }
      internal TextUploadItem(CrypterDB db)
      {
         Db = db;
      }
      public async Task InsertAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"INSERT INTO `TextUploadItems` (`UntrustedName`, `UserID`, `Size`, `TimeStamp`, `CharCount`, `Message`) VALUES (@untrustedname, @userid, @size, @timestamp, @charcount, @message);";
         BindParams(cmd);
         await cmd.ExecuteNonQueryAsync();
         Id = (int)cmd.LastInsertedId;
      }

      public async Task UpdateAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"UPDATE `TextUploadItems` SET `UntrustedName` = @untrustedname, `UserID` = @userid, `Size` = @size, `TimeStamp` = @timestamp, `CharCount` = @charcount, `Message`=@message WHERE `Id` = @id;";
         BindParams(cmd);
         BindId(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      public async Task DeleteAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"DELETE FROM `TextUploadItems` WHERE `Id` = @id;";
         BindId(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      private void BindId(MySqlCommand cmd)
      {
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@id",
            DbType = DbType.Int32,
            Value = Id,
         });
      }

      private void BindParams(MySqlCommand cmd)
      {
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@untrustedname",
            DbType = DbType.String,
            Value = UntrustedName,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@userid",
            DbType = DbType.String,
            Value = UserID,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@size",
            DbType = DbType.String,
            Value = Size,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@timestamp",
            DbType = DbType.String,
            Value = TimeStamp,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@charcount",
            DbType = DbType.Int16,
            Value = CharCount,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@message",
            DbType = DbType.String,
            Value = Message,
         });
      }
   }

}