using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;
using System;
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
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`,`EncryptedMessagePath`,`SignaturePath`,`Created`, `ExpirationDate` FROM `MessageUploads` WHERE `ID` = @id";
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
            cmd.CommandText = @"SELECT `ID`, `UserID`, `UntrustedName`, `Size`,`EncryptedMessagePath`,`SignaturePath`,`Created`, `ExpirationDate` FROM `MessageUploads`";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        public async Task DeleteAllAsync()
        {
            using var txn = await Db.Connection.BeginTransactionAsync();
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `MessageUploads`";
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
                    var item = new TextUploadItem()
                    {
                        ID = reader.GetString(0),
                        UserID = reader.GetString(1),
                        UntrustedName = reader.GetString(2),
                        Size = reader.GetInt32(3),
                        CipherTextPath = reader.GetString(4),
                        SignaturePath = reader.GetString(5),
                        Created = reader.GetDateTime(6),
                        ExpirationDate = reader.GetDateTime(7)
                    };
                    items.Add(item);
                }
            }
            return items;
        }
    }
}