using System;
using System.Collections.Generic;
using System.Text;

namespace Crypter.Contracts.DTO
{
    public class UserUploadItem
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public DateTime ExpirationDate { get; set; }

        public UserUploadItem(string id, string filename, DateTime expirationDate)
        {
            Id = id;
            FileName = filename;
            ExpirationDate = expirationDate;
        }
    }
}