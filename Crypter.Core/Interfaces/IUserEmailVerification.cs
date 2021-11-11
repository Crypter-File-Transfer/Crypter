using System;

namespace Crypter.Core.Interfaces
{
   public interface IUserEmailVerification
   {
      public Guid Owner { get; set; }
      public Guid Code { get; set; }
      public byte[] VerificationKey { get; set; }
      public DateTime Created { get; set; }
   }
}
