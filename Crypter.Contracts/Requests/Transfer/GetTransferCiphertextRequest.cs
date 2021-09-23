using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Requests
{
   public class GetTransferCiphertextRequest
   {
      public Guid Id { get; set; }
      public string ServerDecryptionKeyBase64 { get; set; }

      [JsonConstructor]
      public GetTransferCiphertextRequest(Guid id, string serverDecryptionKeyBase64)
      {
         Id = id;
         ServerDecryptionKeyBase64 = serverDecryptionKeyBase64;
      }
   }
}
