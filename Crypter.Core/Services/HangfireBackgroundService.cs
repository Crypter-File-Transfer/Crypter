/*
 * Copyright (C) 2023 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.Common.Enums;
using Crypter.Common.Primitives;
using Crypter.Core.Entities;
using Crypter.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IHangfireBackgroundService
   {
      Task SendEmailVerificationAsync(Guid userId, CancellationToken cancellationToken);
      Task SendTransferNotificationAsync(Guid itemId, TransferItemType itemType, CancellationToken cancellationToken);

      /// <summary>
      /// Delete a transfer from transfer storage and the database.
      /// </summary>
      /// <param name="itemId"></param>
      /// <param name="itemType"></param>
      /// <param name="userType"></param>
      /// <param name="deleteFromTransferStorage">
      /// Transfers are streamed from transfer storage to the client.
      /// These streams are sometimes configured to "DeleteOnClose".
      /// The background service should not delete from transfer storage when "DeleteOnClose" is configured.
      /// </param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      Task DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType, bool deleteFromTransferStorage, CancellationToken cancellationToken);
      Task DeleteUserTokenAsync(Guid tokenId, CancellationToken cancellationToken);
      Task DeleteFailedLoginAttemptAsync(Guid failedAttemptId, CancellationToken cancellationToken);
   }

   public class HangfireBackgroundService : IHangfireBackgroundService
   {
      private readonly DataContext _context;
      private readonly IUserService _userService;
      private readonly IUserEmailVerificationService _userEmailVerificationService;
      private readonly IEmailService _emailService;
      private readonly ITransferStorageService _transferStorageService;

      public HangfireBackgroundService(
         DataContext context, IUserService userService, IUserEmailVerificationService userEmailVerificationService, IEmailService emailService, ITransferStorageService transferStorageService)
      {
         _context = context;
         _userService = userService;
         _userEmailVerificationService = userEmailVerificationService;
         _emailService = emailService;
         _transferStorageService = transferStorageService;
      }

      public async Task SendEmailVerificationAsync(Guid userId, CancellationToken cancellationToken)
      {
         var verificationParameters = await _userEmailVerificationService.CreateNewVerificationParametersAsync(userId, cancellationToken);
         await verificationParameters.IfSomeAsync(async x =>
         {
            bool deliverySuccess = await _emailService.SendEmailVerificationAsync(x, CancellationToken.None);
            if (deliverySuccess)
            {
               await _userEmailVerificationService.SaveSentVerificationParametersAsync(x, CancellationToken.None);
            }
         });
      }

      public async Task SendTransferNotificationAsync(Guid itemId, TransferItemType itemType, CancellationToken cancellationToken)
      {
         UserEntity recipient = null;

         switch (itemType)
         {
            case TransferItemType.Message:
               recipient = await _context.UserMessageTransfers
                  .Where(x => x.Id == itemId)
                  .Select(x => x.Recipient)
                  .Where(LinqUserExpressions.UserReceivesEmailNotifications())
                  .FirstOrDefaultAsync(cancellationToken);
               break;
            case TransferItemType.File:
               recipient = await _context.UserFileTransfers
                  .Where(x => x.Id == itemId)
                  .Select(x => x.Recipient)
                  .Where(LinqUserExpressions.UserReceivesEmailNotifications())
                  .FirstOrDefaultAsync(cancellationToken);
               break;
            default:
               return;
         }

         if (recipient is null)
         {
            return;
         }

         if (!EmailAddress.TryFrom(recipient.EmailAddress, out var emailAddress))
         {
            return;
         }

         await _emailService.SendTransferNotificationAsync(emailAddress, cancellationToken);
      }

      public async Task DeleteTransferAsync(Guid itemId, TransferItemType itemType, TransferUserType userType, bool deleteFromTransferStorage, CancellationToken cancellationToken)
      {
         bool entityFound = false;

         if (itemType == TransferItemType.Message && userType == TransferUserType.Anonymous)
         {
            var entity = await _context.AnonymousMessageTransfers.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (entity is not null)
            {
               _context.AnonymousMessageTransfers.Remove(entity);
               entityFound = true;
            }
         }
         else if (itemType == TransferItemType.File && userType == TransferUserType.Anonymous)
         {
            var entity = await _context.AnonymousFileTransfers.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (entity is not null)
            {
               _context.AnonymousFileTransfers.Remove(entity);
               entityFound = true;
            }
         }
         else if (itemType == TransferItemType.Message && userType == TransferUserType.User)
         {
            var entity = await _context.UserMessageTransfers.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (entity is not null)
            {
               _context.UserMessageTransfers.Remove(entity);
               entityFound = true;
            }
         }
         else if (itemType == TransferItemType.File && userType == TransferUserType.User)
         {
            var entity = await _context.UserFileTransfers.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (entity is not null)
            {
               _context.UserFileTransfers.Remove(entity);
               entityFound = true;
            }
         }
         else
         {
            // todo
         }

         if (entityFound)
         {
            await _context.SaveChangesAsync(cancellationToken);
         }

         if (deleteFromTransferStorage)
         {
            _transferStorageService.DeleteTransfer(itemId, itemType, userType);
         }
      }

      public async Task DeleteUserTokenAsync(Guid tokenId, CancellationToken cancellationToken)
      {
         await _userService.DeleteUserTokenEntityAsync(tokenId, cancellationToken);
      }

      public async Task DeleteFailedLoginAttemptAsync(Guid failedAttemptId, CancellationToken cancellationToken)
      {
         var foundAttempt = await _context.UserFailedLoginAttempts
            .FirstOrDefaultAsync(x => x.Id == failedAttemptId, cancellationToken);

         if (foundAttempt is not null)
         {
            _context.UserFailedLoginAttempts.Remove(foundAttempt);
            await _context.SaveChangesAsync(cancellationToken);
         }
      }
   }
}
