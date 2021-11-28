using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdateNotificationSettingRequest
   {
      public bool EnableTransferNotifications { get; set; }
      public bool EmailNotifications { get; set; }

      [JsonConstructor]
      public UpdateNotificationSettingRequest(bool enableTransferNotifications, bool emailNotifications)
      {
         EnableTransferNotifications = enableTransferNotifications;
         EmailNotifications = emailNotifications;
      }
   }
}
