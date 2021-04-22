using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
   public class AnonymousDownloadResponse : BaseResponse
   {
      public string CipherText { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      private AnonymousDownloadResponse() : base(StatusCode.Unknown)
      { }

      /// <summary>
      /// Error response
      /// </summary>
      /// <param name="status"></param>
      public AnonymousDownloadResponse(StatusCode status) : base(status)
      { }

      /// <summary>
      /// Success response
      /// </summary>
      /// <param name="cipherText"></param>
      public AnonymousDownloadResponse(string cipherText) : base(StatusCode.Success)
      {
         CipherText = cipherText;
      }
   }
}
