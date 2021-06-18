using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Requests
{
   public class GenericCiphertextRequest
   {
      public Guid Id { get; set; }
      public string ServerDecryptionKeyBase64 { get; set; }

      [JsonConstructor]
      public GenericCiphertextRequest(Guid id, string serverDecryptionKeyBase64)
      {
         Id = id;
         ServerDecryptionKeyBase64 = serverDecryptionKeyBase64;
      }
   }
}
