/*
 * Copyright (C) 2022 Crypter File Transfer
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
using Crypter.Core.Features.User.Commands;
using Crypter.Core.Interfaces;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using MediatR;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public interface IHangfireBackgroundService
   {
      Task SendEmailVerificationAsync(Guid userId, CancellationToken cancellationToken);
      Task SendTransferNotificationAsync(TransferItemType itemType, Guid itemId, CancellationToken cancellationToken);
      Task DeleteUserTokenAsync(Guid tokenId, CancellationToken cancellationToken);
   }

   public class HangfireBackgroundService : IHangfireBackgroundService
   {
      private readonly IBaseTransferService<IMessageTransfer> _messageTransferService;
      private readonly IBaseTransferService<IFileTransfer> _fileTransferService;
      private readonly IUserService _userService;
      private readonly IUserNotificationSettingService _userNotificationSettingService;
      private readonly IUserEmailVerificationService _userEmailVerificationService;
      private readonly IEmailService _emailService;
      private readonly IMediator _mediator;

      public HangfireBackgroundService(IBaseTransferService<IMessageTransfer> messageTransferService, IBaseTransferService<IFileTransfer> fileTransferService,
         IUserService userService, IUserNotificationSettingService userNotificationSettingService, IUserEmailVerificationService userEmailVerificationService,
         IEmailService emailService, IMediator mediator)
      {
         _messageTransferService = messageTransferService;
         _fileTransferService = fileTransferService;
         _userService = userService;
         _userNotificationSettingService = userNotificationSettingService;
         _userEmailVerificationService = userEmailVerificationService;
         _emailService = emailService;
         _mediator = mediator;
      }

      public async Task SendEmailVerificationAsync(Guid userId, CancellationToken cancellationToken)
      {
         var userEntity = await _userService.ReadAsync(userId, cancellationToken);
         var userEmailVerificationEntity = await _userEmailVerificationService.ReadAsync(userId, cancellationToken);

         if (userEntity == null                                         // User does not exist
            || userEntity.EmailVerified                                 // User's email address is already verified
            || userEmailVerificationEntity != null)                     // User already has a UserEmailVerification entity
         {
            return;
         }

         if (!EmailAddress.TryFrom(userEntity.Email, out var emailAddress))
         {
            return;
         }

         var verificationCode = Guid.NewGuid();
         var keys = ECDSA.GenerateKeys();

         var success = await _emailService.SendEmailVerificationAsync(emailAddress, verificationCode, keys.Private, cancellationToken);
         if (success)
         {
            byte[] verificationKey = Encoding.UTF8.GetBytes(keys.Public.ConvertToPEM().Value);
            await _userEmailVerificationService.InsertAsync(userId, verificationCode, verificationKey, cancellationToken);
         }
      }

      public async Task SendTransferNotificationAsync(TransferItemType itemType, Guid itemId, CancellationToken cancellationToken)
      {
         Guid recipientId;

         switch (itemType)
         {
            case TransferItemType.Message:
               var message = await _messageTransferService.ReadAsync(itemId, cancellationToken);
               if (message is null)
               {
                  return;
               }

               recipientId = message.Recipient;
               break;
            case TransferItemType.File:
               var file = await _fileTransferService.ReadAsync(itemId, cancellationToken);
               if (file is null)
               {
                  return;
               }

               recipientId = file.Recipient;
               break;
            default:
               return;
         }

         var user = await _userService.ReadAsync(recipientId, cancellationToken);
         if (user is null || !user.EmailVerified)
         {
            return;
         }

         if (!EmailAddress.TryFrom(user.Email, out var emailAddress))
         {
            return;
         }

         var userNotification = await _userNotificationSettingService.ReadAsync(recipientId, cancellationToken);
         if (userNotification is null
            || !userNotification.EnableTransferNotifications
            || !userNotification.EmailNotifications)
         {
            return;
         }

         await _emailService.SendTransferNotificationAsync(emailAddress, cancellationToken);
      }

      public async Task DeleteUserTokenAsync(Guid tokenId, CancellationToken cancellationToken)
      {
         await _mediator.Send(new DeleteUserTokenCommand(tokenId), cancellationToken);
      }
   }
}
