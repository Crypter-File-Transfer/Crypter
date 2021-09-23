using System;

namespace Crypter.Contracts.DTO
{
   public class UserSentFileDTO
   {
      public Guid Id { get; set; }
      public string FileName { get; set; }
      public Guid RecipientId { get; set; }
      public string RecipientUsername { get; set; }
      public string RecipientAlias { get; set; }
      public DateTime ExpirationUTC { get; set; }

      public UserSentFileDTO(Guid id, string fileName, Guid recipientId, string recipientUsername, string recipientAlias, DateTime expirationUTC)
      {
         Id = id;
         FileName = fileName;
         RecipientId = recipientId;
         RecipientUsername = recipientUsername;
         RecipientAlias = recipientAlias;
         ExpirationUTC = expirationUTC;
      }
   }
}
