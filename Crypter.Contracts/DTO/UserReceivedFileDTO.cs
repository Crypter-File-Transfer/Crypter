using System;

namespace Crypter.Contracts.DTO
{
   public class UserReceivedFileDTO
   {
      public Guid Id { get; set; }
      public string FileName { get; set; }
      public Guid SenderId { get; set; }
      public string SenderUsername { get; set; }
      public string SenderPublicAlias { get; set; }
      public DateTime ExpirationUTC { get; set; }

      public UserReceivedFileDTO(Guid id, string fileName, Guid senderId, string senderUsername, string senderPublicAlias, DateTime expirationUTC)
      {
         Id = id;
         FileName = fileName;
         SenderId = senderId;
         SenderUsername = senderUsername;
         SenderPublicAlias = senderPublicAlias;
         ExpirationUTC = expirationUTC;
      }
   }
}
