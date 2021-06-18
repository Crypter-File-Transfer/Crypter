using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdateUserPrivacyRequest
   {
      public string PublicAlias { get; set; }
      public bool IsPublic { get; set; }
      public bool AllowAnonymousMessages { get; set; }
      public bool AllowAnonymousFiles { get; set; }

      [JsonConstructor]
      public UpdateUserPrivacyRequest(string publicAlias, bool isPublic, bool allowAnonymousMessages, bool allowAnonymousFiles)
      {
         PublicAlias = publicAlias;
         IsPublic = isPublic;
         AllowAnonymousMessages = allowAnonymousMessages;
         AllowAnonymousFiles = allowAnonymousFiles;
      }
   }
}
