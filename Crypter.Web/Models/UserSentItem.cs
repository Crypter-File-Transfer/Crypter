using System;

namespace Crypter.Web.Models
{
   public class UserSentItem
   {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public Guid RecipientId { get; set; }
      public string RecipientUsername { get; set; }
      public string RecipientPublicAlias { get; set; }
      public DateTime ExpirationUTC { get; set; }
      public bool IsFile { get; set; }
   }
}
