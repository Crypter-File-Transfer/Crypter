using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UpdateUserPrivacyResponse
   {
      public UpdateUserPreferencesResult Result { get; set; }

      [JsonConstructor]
      public UpdateUserPrivacyResponse(UpdateUserPreferencesResult result)
      {
         Result = result;
      }
   }
}
