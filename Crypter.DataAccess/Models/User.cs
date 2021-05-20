using System;

namespace Crypter.DataAccess.Models
{
    public class User
    {
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool IsPublic { get; set; }
        public string PublicAlias { get; set; }
        public bool AllowAnonFiles { get; set; }
        public bool AllowAnonMessages { get; set; }
        public DateTime UserCreated { get; set; }

        public User()
        {
            UserCreated = DateTime.UtcNow;
        }

        public User(string userid, string publicAlias, bool ispublic, bool allowMessages, bool allowFiles)
        {
            UserID = userid;
            PublicAlias = publicAlias;
            IsPublic = ispublic;
            AllowAnonMessages = allowMessages;
            AllowAnonFiles = allowFiles;
        }
    }
}
