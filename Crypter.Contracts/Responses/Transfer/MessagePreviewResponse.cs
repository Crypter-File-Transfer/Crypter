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
      public string SenderAlias { get; set; }
      public Guid RecipientId { get; set; }
      public string X25519PublicKey { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      [JsonConstructor]
      public MessagePreviewResponse(string subject, int size, Guid senderId, string senderUsername, string senderAlias, Guid recipientId, string x25519PublicKey, DateTime creationUTC, DateTime expirationUTC)
      {
         Subject = subject;
         Size = size;
         SenderId = senderId;
         SenderUsername = senderUsername;
         SenderAlias = senderAlias;
         RecipientId = recipientId;
         X25519PublicKey = x25519PublicKey;
         CreationUTC = creationUTC;
         ExpirationUTC = expirationUTC;
      }
   }
}
