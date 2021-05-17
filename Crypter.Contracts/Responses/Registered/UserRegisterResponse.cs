using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UserRegisterResponse : BaseResponse
    {
        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UserRegisterResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public UserRegisterResponse(ResponseCode status) : base(status)
        { }
    }
}
