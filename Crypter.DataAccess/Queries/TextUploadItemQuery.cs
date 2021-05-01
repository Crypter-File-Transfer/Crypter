using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Crypter.DataAccess.Models;
using MySqlConnector;

namespace Crypter.DataAccess.Queries
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
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`,`EncryptedMessagePath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `MessageUploads` WHERE `ID` = @id";
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
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`,`EncryptedMessagePath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `MessageUploads`";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        public async Task<List<TextUploadItem>> FindExpiredItemsAsync()
        {
            using var cmd = Db.Connection.CreateCommand();
            //Used for testing, selects all messages rather than just expired ones, would rather not delete yet
            //cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`, `EncryptedMessagePath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `MessageUploads`";
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`, `EncryptedMessagePath`,`SignaturePath`,`Created`, `ExpirationDate`, `Iv` FROM `MessageUploads` WHERE utc_timestamp() > `ExpirationDate`";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        private async Task<List<TextUploadItem>> ReadAllAsync(DbDataReader reader)
        {
            var items = new List<TextUploadItem>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    var item = new TextUploadItem()
                    {
                        ID = reader.GetString(0),
                        UserID = reader.GetString(1),
                        FileName = reader.GetString(2),
                        Size = reader.GetInt32(3),
                        CipherTextPath = reader.GetString(4),
                        SignaturePath = reader.GetString(5),
                        Created = reader.GetDateTime(6),
                        ExpirationDate = reader.GetDateTime(7),
                        InitializationVector = reader.GetString(8)
                    };
                    items.Add(item);
                }
            }
            return items;
        }

        public long GetSumOfSize()
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(Size), 0) FROM MessageUploads;";
            using var reader = cmd.ExecuteReader();
            reader.Read();
            var result = reader.GetInt64(0);
            return result;
        }
    }
}