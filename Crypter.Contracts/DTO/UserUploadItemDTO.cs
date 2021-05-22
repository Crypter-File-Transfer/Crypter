using Crypter.Contracts.Enum;
using System;

namespace Crypter.Contracts.DTO
{
    public class UserUploadItemDTO
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public ResourceType ItemType { get; set; }
        public DateTime ExpirationDate { get; set; }

        public UserUploadItemDTO(string id, string filename, ResourceType itemType, DateTime expirationDate)
        {
            Id = id;
            FileName = filename;
            ItemType = itemType;
            ExpirationDate = expirationDate;
        }
    }
}