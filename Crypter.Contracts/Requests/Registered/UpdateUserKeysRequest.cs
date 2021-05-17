namespace Crypter.Contracts.Requests.Registered
{
    public class UpdateUserKeysRequest
    {
        public string EncryptedPrivateKey { get; set; }
        public string PublicKey { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UpdateUserKeysRequest()
        { }

        public UpdateUserKeysRequest(string encryptedPrivateKey, string publicKey)
        {
            EncryptedPrivateKey = encryptedPrivateKey;
            PublicKey = publicKey;
        }
    }
}
