using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredTogglePublicUserRequest 
    {
        public string PublicAlias { get; set; }
        public bool SetIsPublic { get; set; }
        public bool AllowAnonMessages { get; set;  }
        public bool AllowAnonFiles { get; set; }
    }
}
