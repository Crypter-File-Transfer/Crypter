using System;

namespace Crypter.DataAccess.Models
{
    public class User
    {
        //unique identifier for users
        public string UserID { get; set; }
        // user chosen user name
        public string UserName { get; set; }
        public string Email { get; set; }
        //// hash of user password
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
