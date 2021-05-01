using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses
{
   public class BaseResponse
   {
      public ResponseCode Status { get; set; }
      public string StatusMessage { get; set; }

      /// <summary>
      /// Do not use!
      /// For deserialization purposes only.
      /// </summary>
      protected BaseResponse()
      { }

      public BaseResponse(ResponseCode status)
      {
         Status = status;
         StatusMessage = status.ToString();
      }
   }
}
