using System;

namespace Crypter.Contracts.DTO
{
   public class UserSentMessageDTO
   {
      public Guid Id { get; set; }
      public string Subject { get; set; }
      public Guid RecipientId { get; set; }
      public string RecipientUsername { get; set; }
      public string RecipientAlias { get; set; }
      public DateTime ExpirationUTC { get; set; }

      public UserSentMessageDTO(Guid id, string subject, Guid recipientId, string recipientUsername, string recipientAlias, DateTime expirationUTC)
      {
         Id = id;
         Subject = subject;
         RecipientId = recipientId;
         RecipientUsername = recipientUsername;
         RecipientAlias = recipientAlias;
         ExpirationUTC = expirationUTC;
      }
   }
}
