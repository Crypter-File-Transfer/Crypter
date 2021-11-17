using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserNotificationSettingService : IUserNotificationSettingService
   {
      private readonly DataContext _context;

      public UserNotificationSettingService(DataContext context)
      {
         _context = context;
      }

      public async Task UpsertAsync(Guid userId, bool enableTransferNotifications, bool emailNotifications)
      {
         var userNotificationSettings = await ReadAsync(userId);
         if (userNotificationSettings == null)
         {
            var newUserNotificationSettings = new UserNotificationSetting(userId, enableTransferNotifications, emailNotifications);
            _context.UserNotificationSetting.Add(newUserNotificationSettings);
         }
         else
         {
            userNotificationSettings.EnableTransferNotifications = enableTransferNotifications;
            userNotificationSettings.EmailNotifications = emailNotifications;
         }

         await _context.SaveChangesAsync();
      }

      public async Task<IUserNotificationSetting> ReadAsync(Guid userId)
      {
         return await _context.UserNotificationSetting.FindAsync(userId);
      }
   }
}
