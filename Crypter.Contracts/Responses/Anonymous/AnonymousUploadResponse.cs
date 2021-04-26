using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousUploadResponse : BaseResponse
   {
      public Guid Id { get; set; }
      public DateTime ExpirationUTC { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      private AnonymousUploadResponse() : base(ResponseCode.Unknown)
      { }

      /// <summary>
      /// Error response
      /// </summary>
      /// <param name="status"></param>
      public AnonymousUploadResponse(ResponseCode status) : base(status)
      { }

      /// <summary>
      /// Success response
      /// </summary>
      /// <param name="status"></param>
      /// <param name="id"></param>
      /// <param name="expirationUTC"></param>
      public AnonymousUploadResponse(Guid id, DateTime expirationUTC) : base(ResponseCode.Success)
      {
         Id = id;
         ExpirationUTC = expirationUTC;
      }
   }
}
