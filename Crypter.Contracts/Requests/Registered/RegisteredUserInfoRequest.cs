using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredUserInfoRequest
    {
        public string Id { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisteredUserInfoRequest()
        {
        }

        public RegisteredUserInfoRequest(string id, string token)
        {
            Id = id;
            Token = token; 
        }
    }
}
