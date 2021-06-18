using Crypter.Contracts.DTO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Crypter.Contracts.Responses
{
   public class UserSearchResponse
   {
      public int Total { get; set; }
      public IEnumerable<UserSearchResultDTO> Result { get; set; }

      [JsonConstructor]
      public UserSearchResponse(int total, IEnumerable<UserSearchResultDTO> result)
      {
         Total = total;
         Result = result;
      }
   }
}
