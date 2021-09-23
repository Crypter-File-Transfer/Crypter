using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdatePrivacyRequest
   {
      public bool AllowKeyExchangeRequests { get; set; }
      public UserVisibilityLevel VisibilityLevel { get; set; }
      public UserItemTransferPermission MessageTransferPermission { get; set; }
      public UserItemTransferPermission FileTransferPermission { get; set; }

      [JsonConstructor]
      public UpdatePrivacyRequest(bool allowKeyExchangeRequests, UserVisibilityLevel visibilityLevel, UserItemTransferPermission messageTransferPermission, UserItemTransferPermission fileTransferPermission)
      {
         AllowKeyExchangeRequests = allowKeyExchangeRequests;
         VisibilityLevel = visibilityLevel;
         MessageTransferPermission = messageTransferPermission;
         FileTransferPermission = fileTransferPermission;
      }
   }
}
