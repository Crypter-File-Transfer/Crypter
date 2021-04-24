using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses
{
   public class BaseResponse
   {
      public ResponseCode Status { get; }
      public string StatusMessage { get; }

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
