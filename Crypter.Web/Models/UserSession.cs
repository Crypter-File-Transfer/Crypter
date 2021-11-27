using System;

namespace Crypter.Web.Models
{
   public class UserSession
   {
      public Guid UserId { get; set; }
      public string Username { get; set; }
      public string Token { get; set; }
      public DateTime Expiration { get; set; }

      public UserSession(Guid userId, string username, string token, DateTime expiration)
      {
         UserId = userId;
         Username = username;
         Token = token;
         Expiration = expiration;
      }
   }
}
