using System;

namespace Crypter.Web.Models
{
   public class User
   {
      public Guid Id { get; set; }
      public string Token { get; set; }
      public string X25519PrivateKey { get; set; }
      public string Ed25519PrivateKey { get; set; }

      public User(Guid id, string token, string x25519PrivateKey = null, string ed25519PrivateKey = null)
      {
         Id = id;
         Token = token;
         X25519PrivateKey = x25519PrivateKey;
         Ed25519PrivateKey = ed25519PrivateKey;
      }
   }
}
