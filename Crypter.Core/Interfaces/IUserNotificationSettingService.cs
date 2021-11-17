using System;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IUserNotificationSettingService
   {
      Task<IUserNotificationSetting> ReadAsync(Guid userId);
      Task UpsertAsync(Guid userId, bool enableTransferNotifications, bool emailNotifications);
   }
}