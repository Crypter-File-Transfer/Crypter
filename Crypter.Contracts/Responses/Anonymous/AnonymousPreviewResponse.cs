using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousPreviewResponse : BaseResponse
   {
      public Guid Recipient { get; set; }
      public string Name { get; set; }
      public string ContentType { get; set; }
      public int Size { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      public AnonymousPreviewResponse()
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
      public AnonymousPreviewResponse(Guid recipient, string name, string contentType, int size, DateTime creationUTC, DateTime expirationUTC) : base(ResponseCode.Success)
      {
         Recipient = recipient;
         Name = name;
         ContentType = contentType;
         Size = size;
         CreationUTC = creationUTC;
         ExpirationUTC = expirationUTC;
      }
   }
}
