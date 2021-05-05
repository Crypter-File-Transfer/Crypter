using Crypter.Contracts.Enum;
using System;

namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousDownloadRequest
    {
        public Guid Id { get; set; }
        public ResourceType Type { get; set; }
        public string ServerDecryptionKey { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AnonymousDownloadRequest()
        { }

        public AnonymousDownloadRequest(Guid id, ResourceType type, string serverDecryptionKeyBase64)
        {
            Id = id;
            Type = type;
            ServerDecryptionKey = serverDecryptionKeyBase64;
        }
    }
}
