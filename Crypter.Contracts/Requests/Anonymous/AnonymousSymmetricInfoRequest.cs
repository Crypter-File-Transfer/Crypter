using Crypter.Contracts.Enum;
using System;

namespace Crypter.Contracts.Requests.Anonymous
{
    public class AnonymousSymmetricInfoRequest
    {
        public Guid Id { get; set; }
        public ResourceType Type { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public AnonymousSymmetricInfoRequest()
        { }

        public AnonymousSymmetricInfoRequest(Guid id, ResourceType type)
        {
            Id = id;
            Type = type;
        }
    }
}
