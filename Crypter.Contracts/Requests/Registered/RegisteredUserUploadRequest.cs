using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredUserUploadRequest
    {
        public string Name { get; set; }
        public ResourceType Type { get; set; }
        public string ContentType { get; set; }
        public string CipherText { get; set; }
        public string EncryptedSymmetricInfo { get; set; }
        public string Signature { get; set; }
        public string ServerEncryptionKey { get; set; }
        public string PublicKey { get; set; }
        public string RecipientUsername { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisteredUserUploadRequest()
        { }

        public RegisteredUserUploadRequest(string name, ResourceType type, string contentType, string cipherTextBase64, string encryptedSymmetricDataBase64, string signatureBase64, string serverEncryptionKeyBase64, string publicKeyBase64, string recipientUsername)
        {
            Name = name;
            Type = type;
            ContentType = contentType;
            CipherText = cipherTextBase64;
            EncryptedSymmetricInfo = encryptedSymmetricDataBase64;
            Signature = signatureBase64;
            ServerEncryptionKey = serverEncryptionKeyBase64;
            PublicKey = publicKeyBase64;
            RecipientUsername = recipientUsername;
        }
    }
}
