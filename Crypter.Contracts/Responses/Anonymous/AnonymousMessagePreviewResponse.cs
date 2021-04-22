using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousMessagePreviewResponse : BaseResponse
   {
      public int Size { get; set; }
      public DateTime ExpirationUTC { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      private AnonymousMessagePreviewResponse() : base(StatusCode.Unknown)
      { }

      /// <summary>
      /// Error response
      /// </summary>
      /// <param name="status"></param>
      public AnonymousMessagePreviewResponse(StatusCode status) : base(status)
      { }

      /// <summary>
      /// Success response
      /// </summary>
      /// <param name="name"></param>
      /// <param name="size"></param>
      /// <param name="expirationUTC"></param>
      public AnonymousMessagePreviewResponse(int size, DateTime expirationUTC) : base(StatusCode.Success)
      {
         Size = size;
         ExpirationUTC = expirationUTC;
      }
   }
}
