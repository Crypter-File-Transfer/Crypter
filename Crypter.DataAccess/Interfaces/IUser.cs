using System;

namespace Crypter.DataAccess.Interfaces
{
    public interface IUser
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
    }
}
