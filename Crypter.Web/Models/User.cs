using System;

namespace Crypter.Web.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Token { get; set; }
        public string PrivateKey { get; set; }

        public User(Guid id, string token, string privateKey = null)
        {
            Id = id;
            Token = token;
            PrivateKey = privateKey;
        }
    }
}
