using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UpdateNotificationSettingResponse
   {
      public UpdateUserNotificationSettingResult Result { get; set; }
      public string ResultMessage { get; set; }

      [JsonConstructor]
      public UpdateNotificationSettingResponse(UpdateUserNotificationSettingResult result)
      {
         Result = result;
         ResultMessage = result.ToString();
      }
   }
}
