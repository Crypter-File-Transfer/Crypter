using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class UserPublicProfileResponse
   {
      public Guid Id { get; set; }
      public string Username { get; set; }
      public string Alias { get; set; }
      public string About { get; set; }
      public bool AllowKeyExchangeRequests { get; set; }
      public bool Visible { get; set; }
      public bool ReceivesMessages { get; set; }
      public bool ReceivesFiles { get; set; }
      public string PublicDHKey { get; set; }
      public string PublicDSAKey { get; set; }

      [JsonConstructor]
      public UserPublicProfileResponse(Guid id, string username, string alias, string about, bool allowKeyExchangeRequests, bool visible, bool receivesMessages, bool receivesFiles, string publicDHKey, string publicDSAKey)
      {
         Id = id;
         Username = username;
         Alias = alias;
         About = about;
         AllowKeyExchangeRequests = allowKeyExchangeRequests;
         Visible = visible;
         ReceivesMessages = receivesMessages;
         ReceivesFiles = receivesFiles;
         PublicDHKey = publicDHKey;
         PublicDSAKey = publicDSAKey;
      }
   }
}