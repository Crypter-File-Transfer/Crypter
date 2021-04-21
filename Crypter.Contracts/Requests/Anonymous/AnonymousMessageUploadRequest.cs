namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousMessageUploadRequest
    {
        public string CipherText { get; set; }
        public string Signature { get; set; }
        public string ServerEncryptionKey { get; set; }
    }
}
