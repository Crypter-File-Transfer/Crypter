using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UserRegisterResponse
   {
      public InsertUserResult Result { get; set; }
      public string ResultMessage { get; set; }

      [JsonConstructor]
      public UserRegisterResponse(InsertUserResult result)
      {
         Result = result;
         ResultMessage = result.ToString();
      }
   }
}
