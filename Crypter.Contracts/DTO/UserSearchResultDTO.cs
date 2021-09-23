using System;

namespace Crypter.Contracts.DTO
{
    public class UserSearchResultDTO
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Alias { get; set; }

        public UserSearchResultDTO(Guid id, string username, string alias)
        {
            Id = id;
            Username = username;
            Alias = alias;
        }
    }
}
