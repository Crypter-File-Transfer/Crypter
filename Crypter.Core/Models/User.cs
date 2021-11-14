using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("User")]
   public class User : IUser
   {
      [Key]
      public Guid Id { get; set; }
      public string Username { get; set; }
      public string Email { get; set; }
      public byte[] PasswordHash { get; set; }
      public byte[] PasswordSalt { get; set; }
      public bool EmailVerified { get; set; }
      public DateTime Created { get; set; }
      public DateTime LastLogin { get; set; }

      public virtual UserProfile Profile { get; set; }
      public virtual UserPrivacySetting PrivacySetting { get; set; }
      public virtual UserNotificationSetting NotificationSetting { get; set; }

      public User(Guid id, string username, string email, byte[] passwordHash, byte[] passwordSalt, bool emailVerified, DateTime created, DateTime lastLogin)
      {
         Id = id;
         Username = username;
         Email = email;
         PasswordHash = passwordHash;
         PasswordSalt = passwordSalt;
         EmailVerified = emailVerified;
         Created = created;
         LastLogin = lastLogin;
      }
   }
}
