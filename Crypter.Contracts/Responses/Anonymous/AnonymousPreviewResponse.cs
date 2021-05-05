using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousPreviewResponse : BaseResponse
   {
      public string Name { get; set; }
      public string ContentType { get; set; }
      public int Size { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      private AnonymousPreviewResponse()
      { }

      /// <summary>
      /// Error response
      /// </summary>
      /// <param name="status"></param>
      public AnonymousPreviewResponse(ResponseCode status) : base(status)
      { }

      /// <summary>
      /// Success response
      /// </summary>
      /// <param name="name"></param>
      /// <param name="size"></param>
      /// <param name="expirationUTC"></param>
      public AnonymousPreviewResponse(string name, string contentType, int size, DateTime creationUTC, DateTime expirationUTC) : base(ResponseCode.Success)
      {
         Name = name;
         ContentType = contentType;
         Size = size;
         CreationUTC = creationUTC;
         ExpirationUTC = expirationUTC;
      }
   }
}
