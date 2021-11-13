using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class VerifyUserEmailAddressRequest
   {
      public string Code { get; set; }
      public string Signature { get; set; }

      [JsonConstructor]
      public VerifyUserEmailAddressRequest(string code, string signature)
      {
         Code = code;
         Signature = signature;
      }
   }
}
