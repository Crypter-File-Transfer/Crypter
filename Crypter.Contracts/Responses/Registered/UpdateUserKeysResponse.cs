using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UpdateUserKeysResponse : BaseResponse
    {
        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UpdateUserKeysResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public UpdateUserKeysResponse(ResponseCode status) : base(status)
        { }
    }
}
