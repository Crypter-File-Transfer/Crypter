using System.Collections.Generic;
using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UserUploadsResponse : BaseResponse
    {
        public IEnumerable<UserUploadItemDTO> UserUploadsList { get; set; }
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
        /// <param name="userUploadsList"></param>
        public UserUploadsResponse(IEnumerable<UserUploadItemDTO> userUploads) : base(ResponseCode.Success)
        {
            UserUploadsList = userUploads;
        }
    }
}
