using System;

namespace Crypter.DataAccess.Models
{
    public class User
    {
        //unique identifier for users
        public string UserID { get; set; }
        // user chosen user name
        public string UserName { get; set; }
        //// hash of user password
        //private string Password { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public bool IsPublic { get; set; }
        public string PublicAlias { get; set; }
        public bool AllowAnonFiles { get; set; }
        public bool AllowAnonMessages { get; set; }
        public DateTime UserCreated { get; set; }
        public User()
        {
            UserCreated = DateTime.UtcNow;
        }
    }
}
