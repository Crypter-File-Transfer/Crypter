using Crypter.Contracts.Enum;
using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class TransferUploadResponse
   {
      public UploadResult Result { get; set; }
      public Guid Id { get; set; }
      public DateTime ExpirationUTC { get; set; }

      [JsonConstructor]
      public TransferUploadResponse(UploadResult result, Guid id, DateTime expirationUTC)
      {
         Result = result;
         Id = id;
         ExpirationUTC = expirationUTC;
      }
   }
}
