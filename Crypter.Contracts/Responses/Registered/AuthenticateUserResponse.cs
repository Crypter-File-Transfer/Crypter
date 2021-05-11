using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class AuthenticateUserResponse : BaseResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private AuthenticateUserResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public AuthenticateUserResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="id"></param>
        /// <param name="username"></param>
        /// <param name="token"></param>
        public AuthenticateUserResponse(string id, string username, string token) : base(ResponseCode.Success)
        {
            Id = id;
            Username = username;
            Token = token; 
        }
    }
}

