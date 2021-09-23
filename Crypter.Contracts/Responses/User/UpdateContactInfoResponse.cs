using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UpdateContactInfoResponse
   {
      public UpdateContactInfoResult Result { get; set; }

      [JsonConstructor]
      public UpdateContactInfoResponse(UpdateContactInfoResult result)
      {
         Result = result;
      }
   }
}
