using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using Crypter.API.Controllers;

namespace CrypterAPI.Models
{
    //TextUpload inherits from UploadItem
    public class TextUploadItem : UploadItem
    {
        //constructor sets TimeStamp upon instantiation
        public TextUploadItem()
        {
            Created = DateTime.UtcNow;
            ExpirationDate = DateTime.UtcNow.AddHours(24);
        }

        public async Task InsertAsync(CrypterDB db, string baseSaveDirectory)
        {
            using var cmd = db.Connection.CreateCommand();
            //guid as unique identifier
            ID = Guid.NewGuid().ToString();
            //untrustedName is GUID
            UntrustedName = ID;
            // Create file paths and insert these paths
            FilePaths filePaths = new FilePaths(baseSaveDirectory);
            var success = filePaths.SaveFile(UntrustedName, ID, false);
            //add paths to TextUploadItem object
            CipherTextPath = filePaths.ActualPathString;
            SignaturePath = filePaths.SigPathString;
            //write CipherText and Signature to file system at defined paths
            filePaths.WriteToFile(CipherTextPath, CipherText);
            filePaths.WriteToFile(SignaturePath, Signature);
            cmd.CommandText = @"INSERT INTO `MessageUploads` (`ID`,`UserID`,`UntrustedName`,`Size`, `SignaturePath`, `Created`, `ExpirationDate`, `EncryptedMessagePath`) VALUES (@id, @userid, @untrustedname, @size, @signaturepath, @created, @expirationdate, @encryptedmessagepath);";
            BindParams(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(CrypterDB db)
        {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `MessageUploads` SET `UserID` = @userid, `UntrustedName` = @untrustedname, `Size` = @size, `SignaturePath` = @signaturepath, `Created` = @created, `ExpirationDate` = @expirationdate, `EncryptedMessagePath`= @encryptedmessagepath WHERE `ID` = @id;";
            BindParams(cmd);
            //BindId(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(CrypterDB db)
        {
            using var cmd = db.Connection.CreateCommand();
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
                ParameterName = "@signaturepath",
                DbType = DbType.String,
                Value = SignaturePath,
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
                Value = CipherTextPath,
            });
        }

    }

}