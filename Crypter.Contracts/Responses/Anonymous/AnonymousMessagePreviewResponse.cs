using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousMessagePreviewResponse : BaseResponse
   {
      public int Size { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      private AnonymousMessagePreviewResponse()
      { }

      /// <summary>
      /// Error response
      /// </summary>
      /// <param name="status"></param>
      public AnonymousMessagePreviewResponse(ResponseCode status) : base(status)
      { }

      /// <summary>
      /// Success response
      /// </summary>
      /// <param name="name"></param>
      /// <param name="size"></param>
      /// <param name="expirationUTC"></param>
      public AnonymousMessagePreviewResponse(int size, DateTime creationUTC, DateTime expirationUTC) : base(ResponseCode.Success)
      {
         Size = size;
         CreationUTC = creationUTC;
         ExpirationUTC = expirationUTC;
      }
   }
}
