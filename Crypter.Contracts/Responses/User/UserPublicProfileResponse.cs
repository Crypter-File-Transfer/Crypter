using Newtonsoft.Json;

namespace Crypter.Contracts.Responses
{
   public class UserPublicProfileResponse
   {
      public string UserName { get; set; }
      public string PublicAlias { get; set; }
      public bool AllowsFiles { get; set; }
      public bool AllowsMessages { get; set; }
      public string PublicKey { get; set; }

      [JsonConstructor]
      public UserPublicProfileResponse(string username, string publicAlias, bool allowsFiles, bool allowsMessages, string publicKey)
      {
         UserName = username;
         PublicAlias = publicAlias;
         AllowsFiles = allowsFiles;
         AllowsMessages = allowsMessages;
         PublicKey = publicKey;
      }
   }
}