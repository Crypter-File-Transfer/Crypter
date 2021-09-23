using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class GetTransferCiphertextResponse
   {
      public DownloadCiphertextResult Result { get; set; }
      public string CipherTextBase64 { get; set; }
      public string ClientEncryptionIVBase64 { get; set; }

      [JsonConstructor]
      public GetTransferCiphertextResponse(DownloadCiphertextResult result, string cipherTextBase64, string clientEncryptionIVBase64)
      {
         Result = result;
         CipherTextBase64 = cipherTextBase64;
         ClientEncryptionIVBase64 = clientEncryptionIVBase64;
      }
   }
}
