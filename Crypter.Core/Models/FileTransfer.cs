using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("FileTransfer")]
   public class FileTransfer : BaseTransfer
   {
      public string FileName { get; set; }
      public string ContentType { get; set; }

      public FileTransfer(Guid id, Guid sender, Guid recipient, string fileName, string contentType, int size, string clientIV, string signature, string x25519PublicKey, string ed25519PublicKey, byte[] serverIV, byte[] serverDigest, DateTime created, DateTime expiration)
         : base(id, sender, recipient, size, clientIV, signature, x25519PublicKey, ed25519PublicKey, serverIV, serverDigest, created, expiration)
      {
         FileName = fileName;
         ContentType = contentType;
      }
   }
}
