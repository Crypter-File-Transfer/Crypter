using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("UserEd25519KeyPair")]
   public class UserEd25519KeyPair : IUserPublicKeyPair
   {
      public Guid Id { get; set; }
      public Guid Owner { get; set; }
      public string PrivateKey { get; set; }
      public string PublicKey { get; set; }
      public DateTime Created { get; set; }

      public UserEd25519KeyPair(Guid id, Guid owner, string privateKey, string publicKey, DateTime created)
      {
         Id = id;
         Owner = owner;
         PrivateKey = privateKey;
         PublicKey = publicKey;
         Created = created;
      }
   }
}