using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredUserPublicSettingsRequest 
    {
        public string UserID { get; set; }
        public string PublicAlias { get; set; }
        public bool SetIsPublic { get; set; }
        public bool AllowAnonMessages { get; set;  }
        public bool AllowAnonFiles { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisteredUserPublicSettingsRequest()
        { }

        public RegisteredUserPublicSettingsRequest(string userid, string publicalias, bool setispublic, bool allowanonmessages, bool allowanonfiles, string token)
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
