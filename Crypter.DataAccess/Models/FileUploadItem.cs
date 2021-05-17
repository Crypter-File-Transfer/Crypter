using System;
using System.Data;
using System.Threading.Tasks;
using Crypter.DataAccess.Helpers;
using MySqlConnector;

namespace Crypter.DataAccess.Models
{
    // FileUpload inherits from UploadItem
    public class FileUploadItem : UploadItem
    {
        public string ContentType { get; set; }

        //constructor sets TimeStamp upon instantiation
        public FileUploadItem()
        {
            Created = DateTime.UtcNow;
            ExpirationDate = DateTime.UtcNow.AddHours(24);
        }
 
        public async Task InsertAsync(CrypterDB db, string baseSaveDirectory)
        {
            using var cmd = db.Connection.CreateCommand();
            //guid as unique identifier
            ID = Guid.NewGuid().ToString();
            //convert serverEncryption key from string to bytes and apply encryption to cipherText
            // decode encryption key from base64 to bytes
            byte[] HashedSymmetricEncryptionKey = Convert.FromBase64String(ServerEncryptionKey);
            //generate an iv and save to UploadItem
            byte[] iv = CryptoLib.BouncyCastle.SymmetricMethods.GenerateIV();
            InitializationVector = Convert.ToBase64String(iv); 
            //make symmetric crypto parameters and apply AES encryption
            var symParams = CryptoLib.Common.MakeSymmetricCryptoParams(HashedSymmetricEncryptionKey, iv);
            byte[] cipherTextAES = CryptoLib.Common.DoSymmetricEncryption(Convert.FromBase64String(CipherText), symParams);
            // Create file paths and insert these paths
            CreateFilePaths filePath = new CreateFilePaths(baseSaveDirectory);
            var success = filePath.SaveToFileSystem(ID, cipherTextAES, Signature, true);
            //add paths to FileUploadItem object
            CipherTextPath = filePath.ActualPathString;
            SignaturePath = filePath.SigPathString;
            // Calc size of cipher text file
            Size = filePath.FileSizeBytes(CipherTextPath); 
            cmd.CommandText = @"INSERT INTO `FileUploads` (`ID`,`UserID`,`UntrustedName`,`Size`, `ContentType`, `SignaturePath`, `Created`, `ExpirationDate`, `EncryptedFileContentPath`, `Iv`) VALUES (@id, @userid, @untrustedname, @size, @contentType, @signaturepath, @created, @expirationdate, @encryptedfilecontentpath, @initializationvector);";
            BindParams(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(CrypterDB db)
        {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `FileUploads` SET `UserID` = @userid, `UntrustedName` = @untrustedname, `Size` = @size, `ContentType` = @contentType, `SignaturePath` = @signaturepath, `Created` = @created, `ExpirationDate` = @expirationdate, `EncryptedFileContentPath`= @encryptedfilecontentpath, `Iv` = @initializationvector WHERE `ID` = @id;";
            BindParams(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(CrypterDB db)
        {
            using var cmd = db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `FileUploads` WHERE `ID` = @id;";
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
                Value = FileName,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@size",
                DbType = DbType.Int16,
                Value = Size,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@contentType",
                DbType = DbType.String,
                Value = ContentType
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
                Value = CipherTextPath,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@initializationvector",
                DbType = DbType.String,
                Value = InitializationVector,
            });
        }
    }
}