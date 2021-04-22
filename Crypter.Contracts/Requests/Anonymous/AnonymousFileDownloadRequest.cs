using System;

namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousFileDownloadRequest
    {
        public Guid Id { get; set; }
        public string ServerDecryptionKey { get; set; }
    }
}
