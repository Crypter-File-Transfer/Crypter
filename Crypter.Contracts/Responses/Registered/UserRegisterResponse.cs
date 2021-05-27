using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UserRegisterResponse : BaseResponse
    {
        public InsertUserResult Result { get; set; }
        public string ResultMessage { get; set; }

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

        public UserRegisterResponse(InsertUserResult result) : base(ResponseCode.Success)
        {
            Result = result;
            ResultMessage = result.ToString();
        }
    }
}
