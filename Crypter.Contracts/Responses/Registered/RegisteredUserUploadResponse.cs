using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisteredUserUploadResponse : BaseResponse
    {
        public Guid Id { get; set; }
        public DateTime ExpirationUTC { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private RegisteredUserUploadResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public RegisteredUserUploadResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="id"></param>
        /// <param name="expirationUTC"></param>
        public RegisteredUserUploadResponse(Guid id, DateTime expirationUTC) : base(ResponseCode.Success)
        {
            Id = id;
            ExpirationUTC = expirationUTC;
        }
    }
}
