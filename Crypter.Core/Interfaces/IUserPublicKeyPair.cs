using System;

namespace Crypter.Core.Interfaces
{
   public interface IUserPublicKeyPair
   {
      public Guid Id { get; set; }
      public Guid Owner { get; set; }
      public string PrivateKey { get; set; }
      public string PublicKey { get; set; }
      public DateTime Created { get; set; }
   }
}
