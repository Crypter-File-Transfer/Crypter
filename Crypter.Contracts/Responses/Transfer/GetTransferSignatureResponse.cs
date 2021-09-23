using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class GetTransferSignatureResponse
   {
      public DownloadSignatureResult Result { get; set; }
      public string SignatureBase64 { get; set; }
      public string Ed25519PublicKeyBase64 { get; set; }

      [JsonConstructor]
      public GetTransferSignatureResponse(DownloadSignatureResult result, string signatureBase64, string ed25519PublicKeyBase64)
      {
         Result = result;
         SignatureBase64 = signatureBase64;
         Ed25519PublicKeyBase64 = ed25519PublicKeyBase64;
      }
   }
}
