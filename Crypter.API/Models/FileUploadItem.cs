using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace CrypterAPI.Models
{
   // FileUpload inherits from UploadItem
   public class FileUploadItem : UploadItem
   {
      public string EncryptedFileContentPath { get; set; }

      //constructor sets TimeStamp upon instantiation
      public FileUploadItem()
      {
        this.Created = DateTime.UtcNow;
        this.ExpirationDate = DateTime.UtcNow.AddHours(24);
      }
      internal FileUploadItem(CrypterDB db)
      {
         Db = db;
      }
      public async Task InsertAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"INSERT INTO `FileUploads` (`UserID`,`UntrustedName`,`Size`, `Signature`, `Created`, `ExpirationDate`, `EncryptedFileContentPath`) VALUES (@userid, @untrustedname, @size, @signature, @created, @expirationdate, @encryptedfilecontentpath);";
         BindParams(cmd);
         await cmd.ExecuteNonQueryAsync();
         //guid as unique identifier
         Id = Guid.NewGuid().ToString();
        }

      public async Task UpdateAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"UPDATE `FileUploads` SET `UserID` = @userid, `UntrustedName` = @untrustedname, `Size` = @size, `Signature` = @signature, `Created` = @created, `ExpirationDate` = @expirationdate, `EncryptedFileContentPath`= @encryptedfilecontentpath WHERE `Id` = @id;";
         BindParams(cmd);
         BindId(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      public async Task DeleteAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"DELETE FROM `FileUploads` WHERE `Id` = @id;";
         BindId(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      private void BindId(MySqlCommand cmd)
      {
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@id",
            DbType = DbType.String,
            Value = Id,
         });
      }

      private void BindParams(MySqlCommand cmd)
       { 
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@userid",
            DbType = DbType.String,
            Value = UserID,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@untrustedname",
            DbType = DbType.String,
            Value = UntrustedName,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@size",
            DbType = DbType.String,
            Value = Size,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@signature",
            DbType = DbType.String,
            Value = Signature,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@created",
            DbType = DbType.String,
            Value = Created,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@expirationdate",
            DbType = DbType.String,
            Value = ExpirationDate,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@encryptedfilecontentpath",
            DbType = DbType.String,
            Value = EncryptedFileContentPath,
         });
      }
   }
}