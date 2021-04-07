using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace CrypterAPI.Models
{
   // FileUpload inherits from UploadItem
   public class FileUploadItem : UploadItem
   {
      //file content to be array of bytes
      //public byte[] FileContent { get; set; }
      public string FileContent { get; set; }

      //constructor sets TimeStamp upon instantiation
      public FileUploadItem()
      {
         this.TimeStamp = DateTime.UtcNow;
      }
      internal FileUploadItem(CrypterDB db)
      {
         Db = db;
      }
      public async Task InsertAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"INSERT INTO `FileUploadItems` (`UntrustedName`, `UserID`, `Size`, `TimeStamp`,`FileContent`) VALUES (@untrustedname, @userid, @size, @timestamp, @filecontent);";
         BindParams(cmd);
         await cmd.ExecuteNonQueryAsync();
         Id = (int)cmd.LastInsertedId;
      }

      public async Task UpdateAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"UPDATE `FileUploadItems` SET `UntrustedName` = @untrustedname, `UserID` = @userid, `Size` = @size, `TimeStamp` = @timestamp, `FileContent`= @filecontent WHERE `Id` = @id;";
         BindParams(cmd);
         BindId(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      public async Task DeleteAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"DELETE FROM `FileUploadItems` WHERE `Id` = @id;";
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
            ParameterName = "@filecontent",
            DbType = DbType.String,
            Value = FileContent,
         });

      }
   }
}