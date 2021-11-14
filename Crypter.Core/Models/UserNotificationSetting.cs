using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("UserNotificationSetting")]
   public class UserNotificationSetting : IUserNotificationSetting
   {
      [Key]
      [ForeignKey("User")]
      public Guid Owner { get; set; }
      public bool EnableTransferNotifications { get; set; }
      public bool EmailNotifications { get; set; }

      public virtual User User { get; set; }

      public UserNotificationSetting(Guid owner, bool enableTransferNotifications, bool emailNotifications)
      {
         Owner = owner;
         EnableTransferNotifications = enableTransferNotifications;
         EmailNotifications = emailNotifications;
      }
   }
}
