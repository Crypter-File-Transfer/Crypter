using System;

namespace Crypter.DataAccess.Models
{
    public class UploadItem
    {
        public string ID { get; set; }
        public string UserID { get; set; }
        public string FileName { get; set; }
        public int Size { get; set; }
        public string CipherText { get; set; }
        public string CipherTextPath { get; set; }
        public string Signature { get; set; }
        public string SignaturePath { get; set; }
        public DateTime Created { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string ServerEncryptionKey { get; set; }
        public string InitializationVector { get; set; }
        public string ServerDigest { get; set; }

        public UploadItem()
        {
            Created = DateTime.UtcNow;
            ExpirationDate = DateTime.UtcNow.AddHours(24);
        }

        public UploadItem(string filename, int size, DateTime expirationDate)
        {
            FileName = filename;
            Size = size;
            ExpirationDate = expirationDate;
        }
    }
}