using System;

namespace Crypter.Core.Interfaces
{
   public interface IUserEmailVerification
   {
      public Guid Owner { get; set; }
      public string VerificationCode { get; set; }
      public DateTime Created { get; set; }
   }
}
