using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
    public class AnonymousDownloadResponse : BaseResponse
    {
        public string CipherText { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AnonymousDownloadResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public AnonymousDownloadResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="cipherText"></param>
        public AnonymousDownloadResponse(string cipherText) : base(ResponseCode.Success)
        {
            CipherText = cipherText;
        }
    }
}
