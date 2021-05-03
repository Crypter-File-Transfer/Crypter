using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisteredCreateUserResponse : BaseResponse
    {
        public string UserName { get; set; }
        public DateTime UserCreatedUTC { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private RegisteredCreateUserResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public RegisteredCreateUserResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="userName"></param>
        /// <param name="userCreationUTC"></param>
        public RegisteredCreateUserResponse(string userName, DateTime userCreationUTC) : base(ResponseCode.Success)
        {
            UserName = userName; 
            UserCreatedUTC = userCreationUTC;
        }
    }
}
