using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
namespace CrypterAPI.Models
{
   //TextUpload inherits from UploadItem
   public class TextUploadItem : UploadItem
    { 
      //public string CharCount { get; set; }
      public string EncryptedMessagePath { get; set; }
      //constructor sets TimeStamp upon instantiation
      public TextUploadItem()
      {
        this.Created = DateTime.UtcNow;
        this.ExpirationDate = DateTime.UtcNow.AddHours(24);
       }
      internal TextUploadItem(CrypterDB db)
      {
         Db = db;
      }
      public async Task InsertAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         //guid as unique identifier
         ID = Guid.NewGuid().ToString();
         cmd.CommandText = @"INSERT INTO `MessageUploads` (`ID`,`UserID`,`UntrustedName`,`Size`, `Signature`, `Created`, `ExpirationDate`, `EncryptedMessagePath`) VALUES (@id, @userid, @untrustedname, @size, @signature, @created, @expirationdate, @encryptedmessagepath);";
         BindParams(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      public async Task UpdateAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"UPDATE `MessageUploads` SET `UserID` = @userid, `UntrustedName` = @untrustedname, `Size` = @size, `Signature` = @signature, `Created` = @created, `ExpirationDate` = @expirationdate, `EncryptedMessagePath`= @encryptedmessagepath WHERE `ID` = @id;";
         BindParams(cmd);
         //BindId(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      public async Task DeleteAsync()
      {
         using var cmd = Db.Connection.CreateCommand();
         cmd.CommandText = @"DELETE FROM `MessageUploads` WHERE `ID` = @id;";
         BindId(cmd);
         await cmd.ExecuteNonQueryAsync();
      }

      private void BindId(MySqlCommand cmd)
      {
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@id",
            DbType = DbType.String,
            Value = ID,
         });
      }

      private void BindParams(MySqlCommand cmd)
      { 
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@id",
            DbType = DbType.String,
            Value = ID
         });
         Console.WriteLine(ID);
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
            DbType = DbType.Int32,
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
            DbType = DbType.DateTime,
            Value = Created,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@expirationdate",
            DbType = DbType.DateTime,
            Value = ExpirationDate,
         });
         cmd.Parameters.Add(new MySqlParameter
         {
            ParameterName = "@encryptedmessagepath",
            DbType = DbType.String,
            Value = EncryptedMessagePath,
         });
      }

    }

}