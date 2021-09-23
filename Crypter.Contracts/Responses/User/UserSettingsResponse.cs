using System;
using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UserSettingsResponse
   {
      public string Username { get; set; }
      public string Email { get; set; }
      public string Alias { get; set; }
      public string About { get; set; }
      public UserVisibilityLevel Visibility { get; set; }
      public bool AllowKeyExchangeRequests { get; set; }
      public UserItemTransferPermission MessageTransferPermission { get; set; }
      public UserItemTransferPermission FileTransferPermission { get; set; }
      public DateTime UserCreated { get; set; }

      [JsonConstructor]
      public UserSettingsResponse(string username, string email, string alias, string about, UserVisibilityLevel visibility, bool allowKeyExchangeRequests, UserItemTransferPermission messageTransferPermission, UserItemTransferPermission fileTransferPermission, DateTime userCreated)
      {
         Username = username;
         Email = email;
         Alias = alias;
         About = about;
         Visibility = visibility;
         AllowKeyExchangeRequests = allowKeyExchangeRequests;
         MessageTransferPermission = messageTransferPermission;
         FileTransferPermission = fileTransferPermission;
         UserCreated = userCreated;
      }
   }
}
