using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class FileUploadRequest
   {
      public string FileName { get; set; }
      public string ContentType { get; set; }
      public string CipherTextBase64 { get; set; }
      public string EncryptedSymmetricInfoBase64 { get; set; }
      public string SignatureBase64 { get; set; }
      public string ServerEncryptionKeyBase64 { get; set; }
      public string PublicKeyBase64 { get; set; }
      public string RecipientUsername { get; set; }

      [JsonConstructor]
      public FileUploadRequest(string fileName, string contentType, string cipherTextBase64, string encryptedSymmetricInfoBase64, string signatureBase64, string serverEncryptionKeyBase64, string publicKeyBase64, string recipientUsername = null)
      {
         FileName = fileName;
         ContentType = contentType;
         CipherTextBase64 = cipherTextBase64;
         EncryptedSymmetricInfoBase64 = encryptedSymmetricInfoBase64;
         SignatureBase64 = signatureBase64;
         ServerEncryptionKeyBase64 = serverEncryptionKeyBase64;
         PublicKeyBase64 = publicKeyBase64;
         RecipientUsername = recipientUsername;
      }
   }
}
