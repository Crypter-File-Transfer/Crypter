using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserPrivacySettingService : IUserPrivacySettingService
   {
      private readonly DataContext _context;

      public UserPrivacySettingService(DataContext context)
      {
         _context = context;
      }

      public async Task<bool> UpsertAsync(Guid userId, bool allowKeyExchangeRequests, UserVisibilityLevel visibilityLevel, UserItemTransferPermission receiveFilesPermission, UserItemTransferPermission receiveMessagesPermission)
      {
         var userPrivacySettings = await ReadAsync(userId);
         if (userPrivacySettings == null)
         {
            var newPrivacySettings = new UserPrivacySetting(userId, allowKeyExchangeRequests, visibilityLevel, receiveFilesPermission, receiveMessagesPermission);
            _context.UserPrivacySetting.Add(newPrivacySettings);
         }
         else
         {
            userPrivacySettings.AllowKeyExchangeRequests = allowKeyExchangeRequests;
            userPrivacySettings.Visibility = visibilityLevel;
            userPrivacySettings.ReceiveFiles = receiveFilesPermission;
            userPrivacySettings.ReceiveMessages = receiveMessagesPermission;
         }

         await _context.SaveChangesAsync();
         return true;
      }

      public async Task<IUserPrivacySetting> ReadAsync(Guid userId)
      {
         return await _context.UserPrivacySetting.FindAsync(userId);
      }

      public async Task<bool> IsUserViewableByPartyAsync(Guid userId, Guid otherPartyId)
      {
         if (userId.Equals(otherPartyId))
         {
            return true;
         }

         var userVisibility = (await _context.UserPrivacySetting
            .Where(x => x.Owner == userId)
            .FirstOrDefaultAsync())
            .Visibility;

         return userVisibility switch
         {
            UserVisibilityLevel.None => false,
            UserVisibilityLevel.Contacts => false,
            UserVisibilityLevel.Authenticated => otherPartyId != Guid.Empty,
            UserVisibilityLevel.Everyone => true,
            _ => false,
         };
      }

      public async Task<bool> DoesUserAcceptMessagesFromOtherPartyAsync(Guid userId, Guid otherPartyId)
      {
         var messageTransferPermission = (await _context.UserPrivacySetting
            .Where(x => x.Owner == userId)
            .FirstOrDefaultAsync())
            .ReceiveMessages;

         return messageTransferPermission switch
         {
            UserItemTransferPermission.None => false,
            UserItemTransferPermission.ExchangedKeys => false,
            UserItemTransferPermission.Contacts => false,
            UserItemTransferPermission.Authenticated => otherPartyId != Guid.Empty,
            UserItemTransferPermission.Everyone => true,
            _ => false,
         };
      }

      public async Task<bool> DoesUserAcceptFilesFromOtherPartyAsync(Guid userId, Guid otherPartyId)
      {
         var fileTransferPermission = (await _context.UserPrivacySetting
            .Where(x => x.Owner == userId)
            .FirstOrDefaultAsync())
            .ReceiveFiles;

         return fileTransferPermission switch
         {
            UserItemTransferPermission.None => false,
            UserItemTransferPermission.ExchangedKeys => false,
            UserItemTransferPermission.Contacts => false,
            UserItemTransferPermission.Authenticated => otherPartyId != Guid.Empty,
            UserItemTransferPermission.Everyone => true,
            _ => false,
         };
      }
   }
}
