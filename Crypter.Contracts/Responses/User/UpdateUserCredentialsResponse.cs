using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UpdateUserCredentialsResponse
   {
      public UpdateUserCredentialsResult Result { get; set; }

      [JsonConstructor]
      public UpdateUserCredentialsResponse(UpdateUserCredentialsResult result)
      {
         Result = result;
      }
   }
}
