/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Contracts.Common;
using Crypter.Contracts.Common.Enum;
using Crypter.Contracts.Features.Transfer.Upload;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.CryptoLib.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public class UploadService
   {
      private readonly long AllocatedDiskSpace;
      private readonly long MaxUploadSize;

      private readonly IBaseTransferService<IMessageTransferItem> MessageTransferService;
      private readonly IBaseTransferService<IFileTransferItem> FileTransferService;
      private readonly IEmailService EmailService;
      private readonly IApiValidationService ApiValidationService;
      private readonly ITransferItemStorageService MessageTransferItemStorageService;
      private readonly ITransferItemStorageService FileTransferItemStorageService;
      private readonly ISimpleEncryptionService SimpleEncryptionService;
      private readonly ISimpleHashService SimpleHashService;
      private readonly Func<byte[], byte[]> ItemDigestFunction;

      public UploadService(
         IConfiguration configuration,
         IBaseTransferService<IMessageTransferItem> messageTransferService,
         IBaseTransferService<IFileTransferItem> fileTransferService,
         IEmailService emailService,
         IApiValidationService apiValidationService,
         ISimpleEncryptionService simpleEncryptionService,
         ISimpleHashService simpleHashService
         )
      {
         AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(2, 30);
         MaxUploadSize = long.Parse(configuration["MaxUploadSizeMB"]) * (long)Math.Pow(2, 20);
         MessageTransferService = messageTransferService;
         FileTransferService = fileTransferService;
         EmailService = emailService;
         ApiValidationService = apiValidationService;
         MessageTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.Message);
         FileTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.File);
         SimpleEncryptionService = simpleEncryptionService;
         SimpleHashService = simpleHashService;
         ItemDigestFunction = SimpleHashService.DigestSha256;
      }

      private async Task<(bool Success, UploadTransferError ErrorCode, IBaseTransferItem? GenericTransferData, byte[]? ServerEncryptedCipherText)> ReceiveTransferAsync(IUploadTransferRequest request, Guid senderId, Guid recipientId, CancellationToken cancellationToken)
      {
         var serverHasSpaceRemaining = await ApiValidationService.IsEnoughSpaceForNewTransferAsync(AllocatedDiskSpace, MaxUploadSize, cancellationToken);
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
            originalCiphertextBytes = Convert.FromBase64String(request.CipherTextBase64);
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
         var serverDigest = ItemDigestFunction(originalCiphertextBytes);

         // Apply server-side encryption
         if (hashedSymmetricEncryptionKey.Length != 32)
         {
            return (false, UploadTransferError.InvalidServerEncryptionKey, null, null);
         }

         var (serverEncryptedCiphertext, serverIV) = SimpleEncryptionService.Encrypt(hashedSymmetricEncryptionKey, originalCiphertextBytes);

         Guid itemId = Guid.NewGuid();
         var created = DateTime.UtcNow;
         var expiration = created.AddHours(request.LifetimeHours);

         var returnItem = new BaseTransfer(itemId, senderId, recipientId, originalCiphertextBytes.Length, request.ClientEncryptionIVBase64, request.SignatureBase64, request.X25519PublicKeyBase64, request.Ed25519PublicKeyBase64, serverIV, serverDigest, created, expiration);
         return (true, UploadTransferError.UnknownError, returnItem, serverEncryptedCiphertext);
      }

      public async Task<IActionResult> ReceiveMessageTransferAsync(UploadMessageTransferRequest request, Guid senderId, Guid recipientId, CancellationToken cancellationToken)
      {
         (var success, var errorCode, var genericTransferData, var ciphertextServerEncrypted) = await ReceiveTransferAsync(request, senderId, recipientId, cancellationToken);

         if (!success || genericTransferData is null)
         {
            return new BadRequestObjectResult(new ErrorResponse(errorCode));
         }

         var saveResult = await MessageTransferItemStorageService.SaveAsync(genericTransferData.Id, ciphertextServerEncrypted, cancellationToken);
         if (!saveResult)
         {
            return new BadRequestObjectResult(new ErrorResponse(UploadTransferError.UnknownError));
         }

         var messageItem = new MessageTransfer(
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

         await MessageTransferService.InsertAsync(messageItem, default);

         if (recipientId != Guid.Empty)
         {
            BackgroundJob.Enqueue(() => EmailService.HangfireSendTransferNotificationAsync(TransferItemType.Message, messageItem.Id));
         }

         return new OkObjectResult(
             new UploadTransferResponse(genericTransferData.Id, genericTransferData.Expiration));
      }

      public async Task<IActionResult> ReceiveFileTransferAsync(UploadFileTransferRequest request, Guid senderId, Guid recipientId, CancellationToken cancellationToken)
      {
         (var success, var errorCode, var genericTransferData, var ciphertextServerEncrypted) = await ReceiveTransferAsync(request, senderId, recipientId, cancellationToken);

         if (!success || genericTransferData is null)
         {
            return new BadRequestObjectResult(new ErrorResponse(errorCode));
         }

         var saveResult = await FileTransferItemStorageService.SaveAsync(genericTransferData.Id, ciphertextServerEncrypted, cancellationToken);
         if (!saveResult)
         {
            return new BadRequestObjectResult(new ErrorResponse(UploadTransferError.UnknownError));
         }

         var fileItem = new FileTransfer(
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

         await FileTransferService.InsertAsync(fileItem, default);

         if (recipientId != Guid.Empty)
         {
            BackgroundJob.Enqueue(() => EmailService.HangfireSendTransferNotificationAsync(TransferItemType.File, fileItem.Id));
         }

         return new OkObjectResult(
             new UploadTransferResponse(genericTransferData.Id, genericTransferData.Expiration));
      }
   }
}
