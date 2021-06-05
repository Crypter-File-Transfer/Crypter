using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Anonymous
{
    public class AnonymousSignatureResponse : BaseResponse
    {
        public string Signature { get; set; }
        public string PublicKey { get; set; }
        public string EncryptedSymmetricInfo { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AnonymousSignatureResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public AnonymousSignatureResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="signatureBase64"></param>
        /// <param name="publicKeyBase64"></param>
        /// <param name="symmetricInfoBase64"></param>
        public AnonymousSignatureResponse(string signatureBase64, string publicKeyBase64, string symmetricInfoBase64) : base(ResponseCode.Success)
        {
            Signature = signatureBase64;
            PublicKey = publicKeyBase64;
            EncryptedSymmetricInfo = symmetricInfoBase64;
        }
    }
}
