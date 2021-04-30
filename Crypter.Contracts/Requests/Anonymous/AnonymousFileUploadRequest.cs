namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousFileUploadRequest
    {
        public string Name { get; set; }
        public string ContentType { get; set; }
        public string CipherText { get; set; }
        public string Signature { get; set; }
        public string ServerEncryptionKey { get; set; }
    }
}
