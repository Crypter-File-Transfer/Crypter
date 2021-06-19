using Crypter.Contracts.Enum;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class GenericSignatureResponse
   {
      public DownloadSignatureResult Result { get; set; }
      public string SignatureBase64 { get; set; }
      public string PublicKeyBase64 { get; set; }
      public string EncryptedSymmetricInfoBase64 { get; set; }

      [JsonConstructor]
      public GenericSignatureResponse(DownloadSignatureResult result, string signatureBase64, string publicKeyBase64, string encryptedSymmetricInfoBase64)
      {
         Result = result;
         SignatureBase64 = signatureBase64;
         PublicKeyBase64 = publicKeyBase64;
         EncryptedSymmetricInfoBase64 = encryptedSymmetricInfoBase64;
      }
   }
}
