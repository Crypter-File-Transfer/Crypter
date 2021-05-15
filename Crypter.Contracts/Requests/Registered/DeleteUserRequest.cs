using System;
namespace Crypter.Contracts.Requests.Registered
{
    public class DeleteUserRequest
    {
        public string UserID { get; set; }
        public string Token { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public DeleteUserRequest()
        { }

        public DeleteUserRequest(string userid, string token)
        {
            UserID = userid;
            Token = token; 
        }
    }
}
