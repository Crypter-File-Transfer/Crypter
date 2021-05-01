using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Crypter.DataAccess.Models;
using MySqlConnector;

namespace Crypter.DataAccess.Queries
{
    public class FileUploadItemQuery
    {
        public CrypterDB Db { get; }
        public FileUploadItemQuery(CrypterDB db)
        {
            Db = db;
        }

        public async Task<FileUploadItem> FindOneAsync(string id)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`, `ContentType`, `EncryptedFileContentPath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `FileUploads` WHERE `ID` = @id";
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.String,
                Value = id,
            });
            var result = await ReadAllAsync(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task<List<FileUploadItem>> LatestItemsAsync()
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`, `ContentType`, `EncryptedFileContentPath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `FileUploads`";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        public async Task<List<FileUploadItem>> FindExpiredItemsAsync()
        {
            using var cmd = Db.Connection.CreateCommand();
            //Used for testing, selects all messages rather than just expired ones, would rather not delete just yet
            //cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`, `ContentType`, `EncryptedFileContentPath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `FileUploads`";
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`, `ContentType`, `EncryptedFileContentPath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `FileUploads` WHERE utc_timestamp() > `ExpirationDate`";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        private async Task<List<FileUploadItem>> ReadAllAsync(DbDataReader reader)
        {
            var items = new List<FileUploadItem>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    var item = new FileUploadItem()
                    {
                        ID = reader.GetString(0),
                        UserID = reader.GetString(1),
                        FileName = reader.GetString(2),
                        Size = reader.GetInt32(3),
                        ContentType = reader.GetString(4),
                        CipherTextPath = reader.GetString(5),
                        SignaturePath = reader.GetString(6),
                        Created = reader.GetDateTime(7),
                        ExpirationDate = reader.GetDateTime(8),
                        InitializationVector = reader.GetString(9)
                    };
                    items.Add(item);
                }
            }
            return items;
        }
    }
}