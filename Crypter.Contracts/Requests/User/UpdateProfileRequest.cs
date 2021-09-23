using Newtonsoft.Json;

namespace Crypter.Contracts.Requests
{
   public class UpdateProfileRequest
   {
      public string Alias { get; set; }
      public string About { get; set; }

      [JsonConstructor]
      public UpdateProfileRequest(string alias, string about)
      {
         Alias = alias;
         About = about;
      }
   }
}
