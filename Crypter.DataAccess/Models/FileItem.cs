using Crypter.DataAccess.Interfaces;
using System;

namespace Crypter.DataAccess.Models
{
    public class FileItem : IBaseItem
    {
        // IBaseItem
        public Guid Id { get; set; }
        public Guid Sender { get; set; }
        public int Size { get; set; }
        public string CipherTextPath { get; set; }
        public string SignaturePath { get; set; }
        public byte[] ServerIV { get; set; }
        public byte[] ServerDigest { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expiration { get; set; }

        // FileItem
        public string FileName { get; set; }
        public string ContentType { get; set; }

        public FileItem(Guid id, Guid sender, string fileName, string contentType, int size, string cipherTextPath, string signaturePath, byte[] serverIV, byte[] serverDigest, DateTime created, DateTime expiration)
        {
            Id = id;
            Sender = sender;
            FileName = fileName;
            ContentType = contentType;
            Size = size;
            CipherTextPath = cipherTextPath;
            SignaturePath = signaturePath;
            ServerIV = serverIV;
            ServerDigest = serverDigest;
            Created = created;
            Expiration = expiration;
        }
    }
}
