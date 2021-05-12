using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredUserInfoRequest
    {
        public string Id { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisteredUserInfoRequest()
        {
        }

        public RegisteredUserInfoRequest(string id)
        {
            Id = id;
        }
    }
}
