using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Requests
{
   public class GenericSignatureRequest
   {
      public Guid Id { get; set; }

      [JsonConstructor]
      public GenericSignatureRequest(Guid id)
      {
         Id = id;
      }
   }
}
