using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class UpdateUserEmailRequest
    {
        public string Id { get; set; }
        public string Email { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public UpdateUserEmailRequest()
        { }

        public UpdateUserEmailRequest(string id, string email)
        {
            Id = id; 
            Email = email;
        }
    }
}
