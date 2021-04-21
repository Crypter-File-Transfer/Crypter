using System;
namespace Crypter.API.Models
{
    public class User
    {
        //unique identifier for users
        public string UserID { get; set; }
        // user chosen user name
        public string UserName { get; set; }
        // hash of user password
        private string Password { get; set; }
        public DateTime UserCreated { get; set; }
        public User()
        {
            this.UserCreated = DateTime.UtcNow; 
        }
    }
}
