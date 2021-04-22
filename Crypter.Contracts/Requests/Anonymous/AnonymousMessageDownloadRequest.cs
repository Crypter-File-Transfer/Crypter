using System;

namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousMessageDownloadRequest
    {
        public Guid Id { get; set; }
        public string ServerDecryptionKey { get; set; }
    }
}
