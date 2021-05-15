using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UserAuthenticateResponse : BaseResponse
    {
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private UserAuthenticateResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public UserAuthenticateResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="token"></param>
        public UserAuthenticateResponse(string token) : base(ResponseCode.Success)
        {
            Token = token;
        }
    }
}

