using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class RegisteredTogglePublicUserResponse : BaseResponse
    {
        public string PublicAlias { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowAnonMessages { get; set; }
        public bool AllowAnonFiles { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private RegisteredTogglePublicUserResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public RegisteredTogglePublicUserResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="status"></param>
        /// <param name="publicAlias"></param>
        /// <param name="isPublic"></param>
        /// <param name="allowMessages"></param>
        /// <param name="allowFiles"></param>
        public RegisteredTogglePublicUserResponse(string publicAlias, bool isPublic, bool allowMessages, bool allowFiles) : base(ResponseCode.Success)
        {
            PublicAlias = publicAlias;
            IsPublic = isPublic;
            AllowAnonMessages = allowMessages;
            AllowAnonFiles = allowFiles; 
        }
    }
}
