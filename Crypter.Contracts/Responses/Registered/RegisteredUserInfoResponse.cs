using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisteredUserInfoResponse : BaseResponse
    {

        public string UserName { get; set; }
        public string Email { get; set; }
        //public string Password { get; set; }
        public bool IsPublic { get; set; }
        public string PublicAlias { get; set; }
        public bool AllowAnonFiles { get; set; }
        public bool AllowAnonMessages { get; set; }
        public DateTime UserCreated { get; set; }
        public string Token { get; set; }


        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private RegisteredUserInfoResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public RegisteredUserInfoResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <param name="isPublic"></param>
        /// <param name="alias"></param>
        /// <param name="allowAnonFiles"></param>
        /// <param name="allowAnonMessages"></param>
        /// <param name="userCreated"></param>
        /// <param name="token"></param>
        public RegisteredUserInfoResponse(string username, string email, bool isPublic, string alias, bool allowAnonFiles, bool allowAnonMessages, DateTime userCreated, string token) : base(ResponseCode.Success)
        { 
            UserName = username;
            Email = email;
            //Password = password;
            IsPublic = isPublic;
            PublicAlias = alias;
            AllowAnonFiles = allowAnonFiles;
            AllowAnonMessages = allowAnonMessages;
            UserCreated = userCreated;
            Token = token; 
        }
    }
}
