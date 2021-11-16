using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("UserPrivacySetting")]
   public class UserPrivacySetting : IUserPrivacySetting
   {
      [Key]
      [ForeignKey("User")]
      public Guid Owner { get; set; }
      public bool AllowKeyExchangeRequests { get; set; }
      public UserVisibilityLevel Visibility { get; set; }
      public UserItemTransferPermission ReceiveFiles { get; set; }
      public UserItemTransferPermission ReceiveMessages { get; set; }

      public virtual User User { get; set; }

      public UserPrivacySetting(Guid owner, bool allowKeyExchangeRequests, UserVisibilityLevel visibility, UserItemTransferPermission receiveFiles, UserItemTransferPermission receiveMessages)
      {
         Owner = owner;
         AllowKeyExchangeRequests = allowKeyExchangeRequests;
         Visibility = visibility;
         ReceiveFiles = receiveFiles;
         ReceiveMessages = receiveMessages;
      }
   }
}
