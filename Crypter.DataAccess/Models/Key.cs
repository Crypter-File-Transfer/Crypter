using System;

namespace Crypter.DataAccess.Models
{
    public enum KeyType
    {
        Personal,
        Exchanged
    }

    public class Key
    {
        public string KeyId { get; set; }
        public string UserId { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public KeyType KeyType { get; set; }
        public DateTime Created { get; set; }

        public Key(string keyId, string userId, string privateKey, string publicKey, KeyType keyType, DateTime created)
        {
            KeyId = keyId;
            UserId = userId;
            PrivateKey = privateKey;
            PublicKey = publicKey;
            KeyType = keyType;
            Created = created;
        }
    }
}