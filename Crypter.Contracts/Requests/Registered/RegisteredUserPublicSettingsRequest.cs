namespace Crypter.Contracts.Requests.Registered
{
    public class RegisteredUserPublicSettingsRequest
    {
        public string PublicAlias { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowAnonymousMessages { get; set; }
        public bool AllowAnonymousFiles { get; set; }

        /// <summary>
        /// Do not use!
        /// For deserialization purposes only.
        /// </summary>
        public RegisteredUserPublicSettingsRequest()
        { }

        public RegisteredUserPublicSettingsRequest(string publicAlias, bool isPublic, bool allowAnonymousMessages, bool allowAnonymousFiles)
        {
            PublicAlias = publicAlias;
            IsPublic = isPublic;
            AllowAnonymousMessages = allowAnonymousMessages;
            AllowAnonymousFiles = allowAnonymousFiles;
        }
    }
}
