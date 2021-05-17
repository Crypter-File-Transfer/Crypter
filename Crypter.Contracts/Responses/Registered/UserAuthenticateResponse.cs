using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class UserAuthenticateResponse : BaseResponse
    {
        public string Id { get; set; }
        public string Token { get; set; }
        public string EncryptedPrivateKey { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UserAuthenticateResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public UserAuthenticateResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <param name="encryptedPrivateKey></param>
        public UserAuthenticateResponse(string id, string token, string encryptedPrivateKey = null) : base(ResponseCode.Success)
        {
            Id = id;
            Token = token;
            EncryptedPrivateKey = encryptedPrivateKey;
        }
    }
}

