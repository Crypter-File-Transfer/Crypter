using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousSignatureResponse : BaseResponse
   {
      public string Signature { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      private AnonymousSignatureResponse() : base(StatusCode.Unknown)
      { }

      /// <summary>
      /// Error response
      /// </summary>
      /// <param name="status"></param>
      public AnonymousSignatureResponse(StatusCode status) : base(status)
      { }

      /// <summary>
      /// Success response
      /// </summary>
      /// <param name="status"></param>
      /// <param name="signatureBase64"></param>
      public AnonymousSignatureResponse(string signatureBase64) : base(StatusCode.Success)
      {
         Signature = signatureBase64;
      }
   }
}
