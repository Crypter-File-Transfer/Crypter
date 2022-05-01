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
using Crypter.Contracts.Common;
using Crypter.Contracts.Features.Transfer.Upload;
using Crypter.Core.Entities;
using Crypter.Core.Entities.Interfaces;
using Crypter.Core.Interfaces;
using Crypter.Core.Services;
using Crypter.CryptoLib.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
#nullable enable // gross

   public interface IUploadService
   {
      Task<IActionResult> ReceiveMessageTransferAsync(UploadMessageTransferRequest request, Guid senderId, string recipient, CancellationToken cancellationToken);
      Task<IActionResult> ReceiveFileTransferAsync(UploadFileTransferRequest request, Guid senderId, string recipient, CancellationToken cancellationToken);
   }

   public class UploadService : IUploadService
   {
      private readonly long _allocatedDiskSpace;
      private readonly long _maxUploadSize;

      private readonly IBaseTransferService<IMessageTransfer> _messageTransferService;
      private readonly IBaseTransferService<IFileTransfer> _fileTransferService;
      private readonly IApiValidationService _apiValidationService;
      private readonly ITransferItemStorageService _messageTransferItemStorageService;
      private readonly ITransferItemStorageService _fileTransferItemStorageService;
      private readonly ISimpleEncryptionService _simpleEncryptionService;
      private readonly ISimpleHashService _simpleHashService;
      private readonly IUserService _userService;
      private readonly IHangfireBackgroundService _hangfireBackgroundService;
      private readonly Func<byte[], byte[]> _itemDigestFunction;

      public UploadService(
         IConfiguration configuration,
         IBaseTransferService<IMessageTransfer> messageTransferService,
         IBaseTransferService<IFileTransfer> fileTransferService,
         IApiValidationService apiValidationService,
         ISimpleEncryptionService simpleEncryptionService,
         IUserService userService,
         ISimpleHashService simpleHashService,
         IHangfireBackgroundService hangfireBackgroundService
         )
      {
         _allocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(2, 30);
         _maxUploadSize = long.Parse(configuration["UploadSettings:MaxUploadSizeMB"]) * (long)Math.Pow(2, 20);
         _messageTransferService = messageTransferService;
         _fileTransferService = fileTransferService;
         _apiValidationService = apiValidationService;
         _messageTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.Message);
         _fileTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.File);
         _simpleEncryptionService = simpleEncryptionService;
         _userService = userService;
         _simpleHashService = simpleHashService;
         _itemDigestFunction = _simpleHashService.DigestSha256;
         _hangfireBackgroundService = hangfireBackgroundService;
      }

      private async Task<(bool Success, UploadTransferError ErrorCode, ITransferBase? GenericTransferData, byte[]? ServerEncryptedCipherText)> ReceiveTransferAsync(IUploadTransferRequest request, Guid senderId, Guid recipientId, CancellationToken cancellationToken)
      {
         var serverHasSpaceRemaining = await _apiValidationService.IsEnoughSpaceForNewTransferAsync(_allocatedDiskSpace, _maxUploadSize, cancellationToken);
         if (!serverHasSpaceRemaining)
         {
            return (false, UploadTransferError.OutOfSpace, null, null);
         }

         byte[] hashedSymmetricEncryptionKey;
         try
         {
            hashedSymmetricEncryptionKey = Convert.FromBase64String(request.ServerEncryptionKeyBase64);
         }
         catch (Exception)
         {
            return (false, UploadTransferError.InvalidServerEncryptionKey, null, null);
         }

         byte[] originalCiphertextBytes;
         try
         {
            var bytePartitions = request.CipherTextBase64
               .Select(x => Convert.FromBase64String(x))
               .ToList();

            int ciphertextLength = bytePartitions
               .Sum(x => x.Length);

            originalCiphertextBytes = new byte[ciphertextLength];

            int currentPosition = 0;
            foreach (var bytePartition in bytePartitions)
            {
               bytePartition.CopyTo(originalCiphertextBytes, currentPosition);
               currentPosition += bytePartition.Length;
            }
         }
         catch (Exception)
         {
            return (false, UploadTransferError.InvalidCipherText, null, null);
         }

         if (request.LifetimeHours > 24 || request.LifetimeHours < 1)
         {
            return (false, UploadTransferError.InvalidRequestedExpiration, null, null);
         }

         // Digest the ciphertext BEFORE applying server-side encryption
      var serverDigest = _itemDigestFunction(originalCiphertextBytes);

         // Apply server-side encryption
         if (hashedSymmetricEncryptionKey.Length != 32)
         {
            return (false, UploadTransferError.InvalidServerEncryptionKey, null, null);
         }

         var (serverEncryptedCiphertext, serverIV) = _simpleEncryptionService.Encrypt(hashedSymmetricEncryptionKey, originalCiphertextBytes);

         Guid itemId = Guid.NewGuid();
         var created = DateTime.UtcNow;
         var expiration = created.AddHours(request.LifetimeHours);

         var returnItem = new BaseTransfer(itemId, senderId, recipientId, originalCiphertextBytes.Length, request.ClientEncryptionIVBase64, request.SignatureBase64, request.X25519PublicKeyBase64, request.Ed25519PublicKeyBase64, serverIV, serverDigest, created, expiration);
         return (true, UploadTransferError.UnknownError, returnItem, serverEncryptedCiphertext);
      }

      public async Task<IActionResult> ReceiveMessageTransferAsync(UploadMessageTransferRequest request, Guid senderId, string recipient, CancellationToken cancellationToken)
      {
         Guid recipientId = Guid.Empty;

         if (!string.IsNullOrEmpty(recipient))
         {
            var maybeUser = await _userService.ReadAsync(recipient, cancellationToken);
            if (maybeUser is null)
            {
               return new BadRequestObjectResult(new ErrorResponse(UploadTransferError.UserNotFound));
            }

            if (maybeUser is not null)
            {
               recipientId = maybeUser.Id;
            }
         }

         (var success, var errorCode, var genericTransferData, var ciphertextServerEncrypted) = await ReceiveTransferAsync(request, senderId, recipientId, cancellationToken);

         if (!success || genericTransferData is null)
         {
            return new BadRequestObjectResult(new ErrorResponse(errorCode));
         }

         var saveResult = await _messageTransferItemStorageService.SaveAsync(genericTransferData.Id, ciphertextServerEncrypted, cancellationToken);
         if (!saveResult)
         {
            return new BadRequestObjectResult(new ErrorResponse(UploadTransferError.UnknownError));
         }

         var messageItem = new MessageTransferEntity(
               genericTransferData.Id,
               senderId,
               recipientId,
               request.Subject,
               genericTransferData.Size,
               genericTransferData.ClientIV,
               genericTransferData.Signature,
               genericTransferData.X25519PublicKey,
               genericTransferData.Ed25519PublicKey,
               genericTransferData.ServerIV,
               genericTransferData.ServerDigest,
               genericTransferData.Created,
               genericTransferData.Expiration);

         await _messageTransferService.InsertAsync(messageItem, default);

         if (recipientId != Guid.Empty)
         {
            BackgroundJob.Enqueue(() => _hangfireBackgroundService.SendTransferNotificationAsync(TransferItemType.Message, messageItem.Id, CancellationToken.None));
         }

         return new OkObjectResult(
             new UploadTransferResponse(genericTransferData.Id, genericTransferData.Expiration));
      }

      public async Task<IActionResult> ReceiveFileTransferAsync(UploadFileTransferRequest request, Guid senderId, string recipient, CancellationToken cancellationToken)
      {
         Guid recipientId = Guid.Empty;

         if (!string.IsNullOrEmpty(recipient))
         {
            var maybeUser = await _userService.ReadAsync(recipient, cancellationToken);

            if (maybeUser is null)
            {
               return new BadRequestObjectResult(new ErrorResponse(UploadTransferError.UserNotFound));
            }

            if (maybeUser is not null)
            {
               recipientId = maybeUser.Id;
            }
         }

         (var success, var errorCode, var genericTransferData, var ciphertextServerEncrypted) = await ReceiveTransferAsync(request, senderId, recipientId, cancellationToken);

         if (!success || genericTransferData is null)
         {
            return new BadRequestObjectResult(new ErrorResponse(errorCode));
         }

         var saveResult = await _fileTransferItemStorageService.SaveAsync(genericTransferData.Id, ciphertextServerEncrypted, cancellationToken);
         if (!saveResult)
         {
            return new BadRequestObjectResult(new ErrorResponse(UploadTransferError.UnknownError));
         }

         var fileItem = new FileTransferEntity(
               genericTransferData.Id,
               senderId,
               recipientId,
               request.FileName,
               request.ContentType,
               genericTransferData.Size,
               genericTransferData.ClientIV,
               genericTransferData.Signature,
               genericTransferData.X25519PublicKey,
               genericTransferData.Ed25519PublicKey,
               genericTransferData.ServerIV,
               genericTransferData.ServerDigest,
               genericTransferData.Created,
               genericTransferData.Expiration);

         await _fileTransferService.InsertAsync(fileItem, default);

         if (recipientId != Guid.Empty)
         {
            BackgroundJob.Enqueue(() => _hangfireBackgroundService.SendTransferNotificationAsync(TransferItemType.File, fileItem.Id, CancellationToken.None));
         }

         return new OkObjectResult(
             new UploadTransferResponse(genericTransferData.Id, genericTransferData.Expiration));
      }
   }
}
