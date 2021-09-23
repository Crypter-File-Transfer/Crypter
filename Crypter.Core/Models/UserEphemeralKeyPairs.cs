using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("UserEphemeralKeyPairs")]
   public class UserEphemeralKeyPairs : IUserEphemeralKeyPairs
   {
      public Guid Id { get; set; }
      public Guid Owner { get; set; }
      public string PrivateKey { get; set; }
      public string PublicKey { get; set; }
      public DateTime Created { get; set; }

      public UserEphemeralKeyPairs(Guid id, Guid owner, string privateKey, string publicKey, DateTime created)
      {
         Id = id;
         Owner = owner;
         PrivateKey = privateKey;
         PublicKey = publicKey;
         Created = created;
      }
   }
}
