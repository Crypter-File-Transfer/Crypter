using System;

namespace CrypterAPI.Models
{
    public class Key
    {
        //unique key in database, will use GUID 
        public string KeyId { get; set; }
        //reference to Users Model
        public string UserId { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        enum KeyType
        {
            Global,
            Exchanged
        }
        // file time stamp
        public DateTime Created { get; set; }
        internal CrypterDB Db { get; set; }
        //constructor sets TimeStamp upon instantiation
        public Key()
        {
            this.Created = DateTime.UtcNow;
        }
        internal Key(CrypterDB db)
        {
            Db = db;
        }
    }
}