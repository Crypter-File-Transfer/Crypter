using System;

namespace Crypter.Core.Interfaces
{
   public interface IUserNotificationSetting
   {
      Guid Owner { get; set; }
      bool EnableTransferNotifications { get; set; }
      bool EmailNotifications { get; set; }
   }
}
