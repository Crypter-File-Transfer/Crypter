using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UserUploadsResponse : BaseResponse
    {
        public string UserUploadsList; 
        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private UserUploadsResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public UserUploadsResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="username"></param>
        public UserUploadsResponse(string userUploadsList) : base(ResponseCode.Success)
        {
            UserUploadsList = userUploadsList; 
        }
    }
}
