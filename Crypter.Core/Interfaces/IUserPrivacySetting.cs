using Crypter.Contracts.Enum;
using System;

namespace Crypter.Core.Interfaces
{
   public interface IUserPrivacySetting
   {
      public Guid Owner { get; set; }
      public bool AllowKeyExchangeRequests { get; set; }
      public UserVisibilityLevel Visibility { get; set; }
      public UserItemTransferPermission ReceiveFiles { get; set; }
      public UserItemTransferPermission ReceiveMessages { get; set; }
   }
}
