using System;

namespace Crypter.Web.Models
{
   public class UserReceivedItem
   {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public Guid SenderId { get; set; }
      public string SenderUsername { get; set; }
      public string SenderPublicAlias { get; set; }
      public DateTime ExpirationUTC { get; set; }
      public bool IsFile { get; set; }
   }
}
