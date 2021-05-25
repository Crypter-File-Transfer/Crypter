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
        public string Signature { get; set; }
        public string SymmetricInfo { get; set; }
        public string PublicKey { get; set; }
        public byte[] ServerIV { get; set; }
        public byte[] ServerDigest { get; set; }
        public DateTime Created { get; set; }
        public DateTime Expiration { get; set; }

        // FileItem
        public string FileName { get; set; }
        public string ContentType { get; set; }

        public FileItem(Guid id, Guid sender, string fileName, string contentType, int size, string cipherTextPath, string signature, string symmetricInfo, string publicKey, byte[] serverIV, byte[] serverDigest, DateTime created, DateTime expiration)
        {
            Id = id;
            Sender = sender;
            FileName = fileName;
            ContentType = contentType;
            Size = size;
            CipherTextPath = cipherTextPath;
            Signature = signature;
            SymmetricInfo = symmetricInfo;
            PublicKey = publicKey;
            ServerIV = serverIV;
            ServerDigest = serverDigest;
            Created = created;
            Expiration = expiration;
        }
    }
}
