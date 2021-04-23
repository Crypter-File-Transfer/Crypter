using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses
{
   public class BaseResponse
   {
      public StatusCode Status { get; }
      public string StatusMessage { get; }

      public BaseResponse(StatusCode status)
      {
         Status = status;
         StatusMessage = status.ToString();
      }
   }
}
