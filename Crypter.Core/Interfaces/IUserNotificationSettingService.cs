using System;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IUserNotificationSettingService
   {
      Task<IUserNotificationSetting> ReadAsync(Guid userId);
      Task<bool> UpsertAsync(Guid userId, bool enableTransferNotifications, bool emailNotifications);
   }
}