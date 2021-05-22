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
        public Guid Id { get; set; }
        public Guid Owner { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public KeyType KeyType { get; set; }
        public DateTime Created { get; set; }

        public Key(Guid id, Guid owner, string privateKey, string publicKey, KeyType keyType, DateTime created)
        {
            Id = id;
            Owner = owner;
            PrivateKey = privateKey;
            PublicKey = publicKey;
            KeyType = keyType;
            Created = created;
        }
    }
}