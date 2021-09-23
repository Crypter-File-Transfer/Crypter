using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("MessageTransfer")]
   public class MessageTransfer : BaseTransfer
   {
      public string Subject { get; set; }

      public MessageTransfer(Guid id, Guid sender, Guid recipient, string subject, int size, string clientIV, string signature, string x25519PublicKey, string ed25519PublicKey, byte[] serverIV, byte[] serverDigest, DateTime created, DateTime expiration)
         : base(id, sender, recipient, size, clientIV, signature, x25519PublicKey, ed25519PublicKey, serverIV, serverDigest, created, expiration)
      {
         Subject = subject;
      }
   }
}
