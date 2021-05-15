using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisteredUserPublicSettingsResponse : BaseResponse
    {
        public string PublicAlias { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowAnonymousMessages { get; set; }
        public bool AllowAnonymousFiles { get; set; }

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
        /// <param name="allowAnonymousMessages"></param>
        /// <param name="allowAnonymousFiles"></param>
        public RegisteredUserPublicSettingsResponse(string publicAlias, bool isPublic, bool allowAnonymousMessages, bool allowAnonymousFiles) : base(ResponseCode.Success)
        {
            PublicAlias = publicAlias;
            IsPublic = isPublic;
            AllowAnonymousMessages = allowAnonymousMessages;
            AllowAnonymousFiles = allowAnonymousFiles;
        }
    }
}
