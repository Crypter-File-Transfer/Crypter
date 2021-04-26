using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousFilePreviewResponse : BaseResponse
   {
      public string Name { get; set; }
      public int Size { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      private AnonymousFilePreviewResponse()
      { }

      /// <summary>
      /// Error response
      /// </summary>
      /// <param name="status"></param>
      public AnonymousFilePreviewResponse(ResponseCode status) : base(status)
      { }

      /// <summary>
      /// Success response
      /// </summary>
      /// <param name="name"></param>
      /// <param name="size"></param>
      /// <param name="expirationUTC"></param>
      public AnonymousFilePreviewResponse(string name, int size, DateTime creationUTC, DateTime expirationUTC) : base(ResponseCode.Success)
      {
         Name = name;
         Size = size;
         CreationUTC = creationUTC;
         ExpirationUTC = expirationUTC;
      }
   }
}
