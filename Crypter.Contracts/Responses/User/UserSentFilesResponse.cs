using Crypter.Contracts.DTO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Crypter.Contracts.Responses
{
   public class UserSentFilesResponse
   {
      public IEnumerable<UserSentFileDTO> Files { get; set; }

      [JsonConstructor]
      public UserSentFilesResponse(IEnumerable<UserSentFileDTO> files)
      {
         Files = files;
      }
   }
}
