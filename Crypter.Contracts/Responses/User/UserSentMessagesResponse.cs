using Crypter.Contracts.DTO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Crypter.Contracts.Responses
{
   public class UserSentMessagesResponse
   {
      public IEnumerable<UserSentMessageDTO> Messages { get; set; }

      [JsonConstructor]
      public UserSentMessagesResponse(IEnumerable<UserSentMessageDTO> messages)
      {
         Messages = messages;
      }
   }
}
