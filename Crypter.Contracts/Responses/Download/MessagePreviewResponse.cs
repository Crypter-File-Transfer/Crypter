using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class MessagePreviewResponse
   {
      public string Subject { get; set; }
      public int Size { get; set; }
      public Guid SenderId { get; set; }
      public string SenderUsername { get; set; }
      public string SenderPublicAlias { get; set; }
      public Guid RecipientId { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      [JsonConstructor]
      public MessagePreviewResponse(string subject, int size, Guid senderId, string senderUsername, string senderPublicAlias, Guid recipientId, DateTime creationUTC, DateTime expirationUTC)
      {
         Subject = subject;
         Size = size;
         SenderId = senderId;
         SenderUsername = senderUsername;
         SenderPublicAlias = senderPublicAlias;
         RecipientId = recipientId;
         CreationUTC = creationUTC;
         ExpirationUTC = expirationUTC;
      }
   }
}
