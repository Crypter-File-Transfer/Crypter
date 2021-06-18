using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Requests
{
   public class GenericPreviewRequest
   {
      public Guid Id { get; set; }

      [JsonConstructor]
      public GenericPreviewRequest(Guid id)
      {
         Id = id;
      }
   }
}
