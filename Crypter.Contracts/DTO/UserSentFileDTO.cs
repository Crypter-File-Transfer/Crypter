using System;

namespace Crypter.Contracts.DTO
{
   public class UserSentFileDTO
   {
      public Guid Id { get; set; }
      public string FileName { get; set; }
      public Guid RecipientId { get; set; }
      public string RecipientUsername { get; set; }
      public string RecipientPublicAlias { get; set; }
      public DateTime ExpirationUTC { get; set; }

      public UserSentFileDTO(Guid id, string fileName, Guid recipientId, string recipientUsername, string recipientPublicAlias, DateTime expirationUTC)
      {
         Id = id;
         FileName = fileName;
         RecipientId = recipientId;
         RecipientUsername = recipientUsername;
         RecipientPublicAlias = recipientPublicAlias;
         ExpirationUTC = expirationUTC;
      }
   }
}
