using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisterUserResponse : BaseResponse
    {
        public string UserName { get; set; }
        public DateTime UserCreatedUTC { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private RegisterUserResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public RegisterUserResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="userName"></param>
        /// <param name="userCreationUTC"></param>
        public RegisterUserResponse(string userName, DateTime userCreationUTC) : base(ResponseCode.Success)
        {
            UserName = userName; 
            UserCreatedUTC = userCreationUTC;
        }
    }
}
