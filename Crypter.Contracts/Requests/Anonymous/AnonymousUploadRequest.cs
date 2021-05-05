using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousUploadRequest
    {
        public string Name { get; set; }
        public ResourceType Type { get; set; }
        public string ContentType { get; set; }
        public string CipherText { get; set; }
        public string Signature { get; set; }
        public string ServerEncryptionKey { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AnonymousUploadRequest()
        { }

        public AnonymousUploadRequest(string name, ResourceType type, string contentType, string cipherTextBase64, string signatureBase64, string serverEncryptionKeyBase64)
        {
            Name = name;
            Type = type;
            ContentType = contentType;
            CipherText = cipherTextBase64;
            Signature = signatureBase64;
            ServerEncryptionKey = serverEncryptionKeyBase64;
        }
    }
}
