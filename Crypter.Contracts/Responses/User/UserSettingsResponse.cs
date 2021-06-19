using System;
using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UserSettingsResponse
   {
      public string UserName { get; set; }
      public string Email { get; set; }
      public bool IsPublic { get; set; }
      public string PublicAlias { get; set; }
      public bool AllowAnonymousFiles { get; set; }
      public bool AllowAnonymousMessages { get; set; }
      public DateTime UserCreated { get; set; }

      [JsonConstructor]
      public UserSettingsResponse(string username, string email, bool isPublic, string publicAlias, bool allowAnonymousFiles, bool allowAnonymousMessages, DateTime userCreated)
      {
         UserName = username;
         Email = email;
         IsPublic = isPublic;
         PublicAlias = publicAlias;
         AllowAnonymousFiles = allowAnonymousFiles;
         AllowAnonymousMessages = allowAnonymousMessages;
         UserCreated = userCreated;
      }
   }
}
