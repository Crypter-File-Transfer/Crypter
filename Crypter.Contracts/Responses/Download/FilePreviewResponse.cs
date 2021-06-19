using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class FilePreviewResponse
   {
      public string FileName { get; set; }
      public string ContentType { get; set; }
      public int Size { get; set; }
      public Guid SenderId { get; set; }
      public string SenderUsername { get; set; }
      public string SenderPublicAlias { get; set; }
      public Guid RecipientId { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      [JsonConstructor]
      public FilePreviewResponse(string fileName, string contentType, int size, Guid senderId, string senderUsername, string senderPublicAlias, Guid recipientId, DateTime creationUTC, DateTime expirationUTC)
      {
         FileName = fileName;
         ContentType = contentType;
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
