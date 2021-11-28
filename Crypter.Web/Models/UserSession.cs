using System;

namespace Crypter.Web.Models
{
   public class UserSession
   {
      public Guid UserId { get; set; }
      public string Username { get; set; }
      public string Token { get; set; }

      public UserSession(Guid userId, string username, string token)
      {
         UserId = userId;
         Username = username;
         Token = token;
      }
   }
}
