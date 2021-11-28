using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserNotificationSettingService : IUserNotificationSettingService
   {
      private readonly DataContext Context;

      public UserNotificationSettingService(DataContext context)
      {
         Context = context;
      }

      public async Task UpsertAsync(Guid userId, bool enableTransferNotifications, bool emailNotifications)
      {
         var userNotificationSettings = await ReadAsync(userId);
         if (userNotificationSettings == null)
         {
            var newUserNotificationSettings = new UserNotificationSetting(userId, enableTransferNotifications, emailNotifications);
            Context.UserNotificationSetting.Add(newUserNotificationSettings);
         }
         else
         {
            userNotificationSettings.EnableTransferNotifications = enableTransferNotifications;
            userNotificationSettings.EmailNotifications = emailNotifications;
         }

         await Context.SaveChangesAsync();
      }

      public async Task<IUserNotificationSetting> ReadAsync(Guid userId)
      {
         return await Context.UserNotificationSetting.FindAsync(userId);
      }
   }
}
