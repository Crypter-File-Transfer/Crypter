namespace Crypter.Contracts.DTO
{
    public class UserSearchResult
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string PublicAlias { get; set; }

        public UserSearchResult(string id, string username, string publicAlias)
        {
            Id = id;
            Username = username;
            PublicAlias = publicAlias;
        }
    }
}
