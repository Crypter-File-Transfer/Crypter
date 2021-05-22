namespace Crypter.Contracts.DTO
{
    public class UserSearchResultDTO
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string PublicAlias { get; set; }

        public UserSearchResultDTO(string id, string username, string publicAlias)
        {
            Id = id;
            Username = username;
            PublicAlias = publicAlias;
        }
    }
}
