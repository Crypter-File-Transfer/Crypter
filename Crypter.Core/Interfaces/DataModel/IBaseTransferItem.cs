using System;

namespace Crypter.Core.Interfaces
{
   public interface IBaseTransferItem
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
   }
}
