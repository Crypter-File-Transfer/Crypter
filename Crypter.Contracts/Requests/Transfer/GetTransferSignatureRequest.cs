using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Requests
{
   public class GetTransferSignatureRequest
   {
      public Guid Id { get; set; }

      [JsonConstructor]
      public GetTransferSignatureRequest(Guid id)
      {
         Id = id;
      }
   }
}
