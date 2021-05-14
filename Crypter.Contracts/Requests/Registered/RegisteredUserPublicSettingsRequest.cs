using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredUserPublicSettingsRequest 
    {
        public string UserID { get; set; }
        public string PublicAlias { get; set; }
        public string SetIsPublic { get; set; }
        public string AllowAnonMessages { get; set;  }
        public string AllowAnonFiles { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisteredUserPublicSettingsRequest()
        { }

        public RegisteredUserPublicSettingsRequest(string userid, string publicalias, string setispublic, string allowanonmessages, string allowanonfiles, string token)
        {
            UserID = userid; 
            PublicAlias = publicalias;
            SetIsPublic = setispublic;
            AllowAnonMessages = allowanonmessages;
            AllowAnonFiles = allowanonfiles;
            Token = token; 
        }
    }
}
