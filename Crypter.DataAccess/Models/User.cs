using Crypter.DataAccess.Interfaces;
using System;

namespace Crypter.DataAccess.Models
{
    public class User : IUser
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PublicAlias { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public bool IsPublic { get; set; }
        public bool AllowAnonymousFiles { get; set; }
        public bool AllowAnonymousMessages { get; set; }
        public DateTime Created { get; set; }

        public User(Guid id, string userName, string email, string publicAlias, byte[] passwordHash, byte[] passwordSalt, bool isPublic, bool allowAnonymousFiles, bool allowAnonymousMessages, DateTime created)
        {
            Id = id;
            UserName = userName;
            Email = email;
            PublicAlias = publicAlias;
            PasswordHash = passwordHash;
            PasswordSalt = passwordSalt;
            IsPublic = isPublic;
            AllowAnonymousFiles = allowAnonymousFiles;
            AllowAnonymousMessages = allowAnonymousMessages;
            Created = created;
        }
    }
}
