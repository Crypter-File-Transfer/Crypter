namespace Crypter.API.Models
{
   public class EmailSettings
   {
      public bool Enabled { get; set; }
      public string From { get; set; }
      public string Username { get; set; }
      public string Password { get; set; }
      public string Host { get; set; }
      public int Port { get; set; }
   }
}
