using Crypter.Core.Interfaces;
using System;

namespace Crypter.Core.Models
{
   public class BaseTransfer : IBaseTransferItem
   {
      public Guid Id { get; set; }
      public Guid Sender { get; set; }
      public Guid Recipient { get; set; }
      public int Size { get; set; }
      public string ClientIV { get; set; }
      public string Signature { get; set; }
      public string X25519PublicKey { get; set; }
      public string Ed25519PublicKey { get; set; }
      public byte[] ServerIV { get; set; }
      public byte[] ServerDigest { get; set; }
      public DateTime Created { get; set; }
      public DateTime Expiration { get; set; }

      public BaseTransfer(Guid id, Guid sender, Guid recipient, int size, string clientIV, string signature, string dhPublicKeyBase64, string dsaPublicKeyBase64, byte[] serverIV, byte[] serverDigest, DateTime created, DateTime expiration)
      {
         Id = id;
         Sender = sender;
         Recipient = recipient;
         Size = size;
         ClientIV = clientIV;
         Signature = signature;
         X25519PublicKey = dhPublicKeyBase64;
         Ed25519PublicKey = dsaPublicKeyBase64;
         ServerIV = serverIV;
         ServerDigest = serverDigest;
         Created = created;
         Expiration = expiration;
      }
   }
}
