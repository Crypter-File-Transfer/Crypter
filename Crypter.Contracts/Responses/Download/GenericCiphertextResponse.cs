using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class GenericCiphertextResponse
   {
      public DownloadCiphertextResult Result { get; set; }
      public string CipherTextBase64 { get; set; }

      [JsonConstructor]
      public GenericCiphertextResponse(DownloadCiphertextResult result, string cipherTextBase64)
      {
         Result = result;
         CipherTextBase64 = cipherTextBase64;
      }
   }
}
