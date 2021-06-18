using Crypter.Contracts.Enum;
using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class GenericUploadResponse
   {
      public UploadResult Result { get; set; }
      public Guid Id { get; set; }
      public DateTime ExpirationUTC { get; set; }

      [JsonConstructor]
      public GenericUploadResponse(UploadResult result, Guid id, DateTime expirationUTC)
      {
         Result = result;
         Id = id;
         ExpirationUTC = expirationUTC;
      }
   }
}
