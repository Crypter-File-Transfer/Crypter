using System;

namespace Crypter.Core.Interfaces
{
   public interface ISchema
   {
      public int Version { get; set; }
      public DateTime Updated { get; set; }
   }
}
