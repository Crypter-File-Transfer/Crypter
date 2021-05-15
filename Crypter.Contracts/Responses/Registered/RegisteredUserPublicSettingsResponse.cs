using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisteredUserPublicSettingsResponse : BaseResponse
    {
        public string PublicAlias { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowAnonMessages { get; set; }
        public bool AllowAnonFiles { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private RegisteredUserPublicSettingsResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public RegisteredUserPublicSettingsResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="publicAlias"></param>
        /// <param name="isPublic"></param>
        /// <param name="allowMessages"></param>
        /// <param name="allowFiles"></param>
        /// <param name="token"></param>
        public RegisteredUserPublicSettingsResponse(string publicAlias, bool isPublic, bool allowMessages, bool allowFiles, string token) : base(ResponseCode.Success)
        {
            PublicAlias = publicAlias;
            IsPublic = isPublic;
            AllowAnonMessages = allowMessages;
            AllowAnonFiles = allowFiles;
            Token = token; 
        }
    }
}
