using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
    public class AnonymousGetPublicProfileResponse : BaseResponse
    {
        public string UserName { get; set; }
        public string PublicAlias { get; set; }
        public bool AllowsFiles { get; set; }
        public bool AllowsMessages { get; set; }
        public string PublicKey { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AnonymousGetPublicProfileResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public AnonymousGetPublicProfileResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="username"></param>
        /// /// <param name="publicAlias"></param>
        public AnonymousGetPublicProfileResponse(string username, string publicAlias, bool allowsFiles, bool allowsMessages, string publicKey) : base(ResponseCode.Success)
        {
            UserName = username;
            PublicAlias = publicAlias;
            AllowsFiles = allowsFiles;
            AllowsMessages = allowsMessages;
            PublicKey = publicKey;
        }
    }
}