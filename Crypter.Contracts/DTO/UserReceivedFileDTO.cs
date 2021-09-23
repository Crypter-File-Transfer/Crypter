using System;

namespace Crypter.Contracts.DTO
{
   public class UserReceivedFileDTO
   {
      public Guid Id { get; set; }
      public string FileName { get; set; }
      public Guid SenderId { get; set; }
      public string SenderUsername { get; set; }
      public string SenderAlias { get; set; }
      public DateTime ExpirationUTC { get; set; }

      public UserReceivedFileDTO(Guid id, string fileName, Guid senderId, string senderUsername, string senderAlias, DateTime expirationUTC)
      {
         Id = id;
         FileName = fileName;
         SenderId = senderId;
         SenderUsername = senderUsername;
         SenderAlias = senderAlias;
         ExpirationUTC = expirationUTC;
      }
   }
}
