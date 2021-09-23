using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Requests
{
   public class GetTransferPreviewRequest
   {
      public Guid Id { get; set; }

      [JsonConstructor]
      public GetTransferPreviewRequest(Guid id)
      {
         Id = id;
      }
   }
}
