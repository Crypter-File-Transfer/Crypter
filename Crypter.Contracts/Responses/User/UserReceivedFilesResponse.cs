using Crypter.Contracts.DTO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Crypter.Contracts.Responses
{
   public class UserReceivedFilesResponse
   {
      public IEnumerable<UserReceivedFileDTO> Files { get; set; }

      [JsonConstructor]
      public UserReceivedFilesResponse(IEnumerable<UserReceivedFileDTO> files)
      {
         Files = files;
      }
   }
}
