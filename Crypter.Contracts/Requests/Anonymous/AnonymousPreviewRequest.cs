using Crypter.Contracts.Enum;
using System;

namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousPreviewRequest
    {
        public Guid Id { get; set; }
        public ResourceType Type { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AnonymousPreviewRequest()
        { }

        public AnonymousPreviewRequest(Guid id, ResourceType type)
        {
            Id = id;
            Type = type;
        }
    }
}
