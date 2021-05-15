using System;
using Crypter.Contracts.Enum;

namespace Crypter.Contracts.Responses.Registered
{
    public class AccountDetailsResponse : BaseResponse
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsPublic { get; set; }
        public string PublicAlias { get; set; }
        public bool AllowAnonymousFiles { get; set; }
        public bool AllowAnonymousMessages { get; set; }
        public DateTime UserCreated { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        private AccountDetailsResponse()
        { }

        /// <summary>
        /// Error response
        /// </summary>
        /// <param name="status"></param>
        public AccountDetailsResponse(ResponseCode status) : base(status)
        { }

        /// <summary>
        /// Success response
        /// </summary>
        /// <param name="username"></param>
        /// <param name="email"></param>
        /// <param name="isPublic"></param>
        /// <param name="alias"></param>
        /// <param name="allowAnonymousFiles"></param>
        /// <param name="allowAnonymousMessages"></param>
        /// <param name="userCreated"></param>
        public AccountDetailsResponse(string username, string email, bool isPublic, string alias, bool allowAnonymousFiles, bool allowAnonymousMessages, DateTime userCreated) : base(ResponseCode.Success)
        { 
            UserName = username;
            Email = email;
            IsPublic = isPublic;
            PublicAlias = alias;
            AllowAnonymousFiles = allowAnonymousFiles;
            AllowAnonymousMessages = allowAnonymousMessages;
            UserCreated = userCreated;
        }
    }
}
