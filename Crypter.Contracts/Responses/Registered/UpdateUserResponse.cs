using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UpdateUserResponse : BaseResponse
    {
        public string Email { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private UpdateUserResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public UpdateUserResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="userName"></param>
        /// <param name="userCreationUTC"></param>
        public UpdateUserResponse(string email) : base(ResponseCode.Success)
        {
            Email = email; 
        }
    }
}
