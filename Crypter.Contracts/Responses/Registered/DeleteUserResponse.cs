using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class DeleteUserResponse : BaseResponse
    {
        public string UserID;
        public string Token;

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private DeleteUserResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public DeleteUserResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="token"></param>
        public DeleteUserResponse(string userid, string token) : base(ResponseCode.Success)
        {
            UserID = userid;
            Token = token;
        }
    }
}
