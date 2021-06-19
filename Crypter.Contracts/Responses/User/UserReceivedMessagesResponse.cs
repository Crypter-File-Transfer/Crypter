using Crypter.Contracts.DTO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Crypter.Contracts.Responses
{
   public class UserReceivedMessagesResponse
   {
      public IEnumerable<UserReceivedMessageDTO> Messages { get; set; }

      [JsonConstructor]
      public UserReceivedMessagesResponse(IEnumerable<UserReceivedMessageDTO> messages)
      {
         Messages = messages;
      }
   }
}
