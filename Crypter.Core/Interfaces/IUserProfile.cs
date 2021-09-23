using System;

namespace Crypter.Core.Interfaces
{
   public interface IUserProfile
   {
      public Guid Owner { get; set; }
      public string Alias { get; set; }
      public string About { get; set; }
      public string Image { set; get; }
   }
}
