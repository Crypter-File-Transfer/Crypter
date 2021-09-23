using System;

namespace Crypter.Core.Interfaces
{
   public interface IUserExchangedKeys
   {
      public Guid Id { get; set; }
      public Guid Owner { get; set; }
      public Guid Target { get; set; }
      public string ReceiveKey { get; set; }
      public string SendKey { get; set; }
      public DateTime Created { get; set; }
   }
}
