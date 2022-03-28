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
using Crypter.Contracts.Features.Transfer.DownloadCiphertext;
using Crypter.Contracts.Features.Transfer.DownloadPreview;
using Crypter.Contracts.Features.Transfer.DownloadSignature;
using Crypter.Core.Interfaces;
using Crypter.Core.Services;
using Crypter.CryptoLib.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public class DownloadService
   {
      private readonly IBaseTransferService<IMessageTransferItem> MessageService;
      private readonly IBaseTransferService<IFileTransferItem> FileService;
      private readonly IUserService UserService;
      private readonly IUserProfileService UserProfileService;
      private readonly ITransferItemStorageService MessageTransferItemStorageService;
      private readonly ITransferItemStorageService FileTransferItemStorageService;
      private readonly ISimpleEncryptionService SimpleEncryptionService;
      private readonly ISimpleHashService SimpleHashService;
      private readonly Func<byte[], byte[]> ItemDigestFunction;

      public DownloadService(
         IConfiguration configuration,
         IBaseTransferService<IMessageTransferItem> messageService,
         IBaseTransferService<IFileTransferItem> fileService,
         IUserService userService,
         IUserProfileService userProfileService,
         ISimpleEncryptionService simpleEncryptionService,
         ISimpleHashService simpleHashService
         )
      {
         MessageService = messageService;
         FileService = fileService;
         UserService = userService;
         UserProfileService = userProfileService;
         MessageTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.Message);
         FileTransferItemStorageService = new TransferItemStorageService(configuration["EncryptedFileStore:Location"], TransferItemType.File);
         SimpleEncryptionService = simpleEncryptionService;
         SimpleHashService = simpleHashService;
         ItemDigestFunction = SimpleHashService.DigestSha256;
      }

      public async Task<IActionResult> GetMessagePreviewAsync(DownloadTransferPreviewRequest request, Guid requestorId, CancellationToken cancellationToken)
      {
         var possibleMessage = await MessageService.ReadAsync(request.Id, cancellationToken);
         if (possibleMessage is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferPreviewError.NotFound));
         }

         string? recipientUsername = null;
         if (possibleMessage.Recipient != Guid.Empty)
         {
            bool messageBelongsToSomeoneElse = possibleMessage.Recipient != requestorId;
            if (messageBelongsToSomeoneElse)
            {
               return new NotFoundObjectResult(new ErrorResponse(DownloadTransferPreviewError.NotFound));
            }

            var possibleUser = await UserService.ReadAsync(requestorId, cancellationToken);
            if (possibleUser != null)
            {
               recipientUsername = possibleUser.Username;
            }
         }

         string? senderUsername = null;
         string? senderAlias = null;
         if (possibleMessage.Sender != Guid.Empty)
         {
            var possibleUser = await UserService.ReadAsync(possibleMessage.Sender, cancellationToken);
            if (possibleUser != null)
            {
               senderUsername = possibleUser.Username;
            }

            var possibleUserProfile = await UserProfileService.ReadAsync(possibleMessage.Sender, cancellationToken);
            if (possibleUserProfile != null)
            {
               senderAlias = possibleUserProfile.Alias;
            }
         }

         return new OkObjectResult(
            new DownloadTransferMessagePreviewResponse(possibleMessage.Subject, possibleMessage.Size, senderUsername, senderAlias, recipientUsername, possibleMessage.X25519PublicKey, possibleMessage.Created, possibleMessage.Expiration));
      }

      public async Task<IActionResult> GetFilePreviewAsync(DownloadTransferPreviewRequest request, Guid requestorId, CancellationToken cancellationToken)
      {
         var possibleFile = await FileService.ReadAsync(request.Id, cancellationToken);
         if (possibleFile is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferPreviewError.NotFound));
         }

         string? recipientUsername = null;
         if (possibleFile.Recipient != Guid.Empty)
         {
            bool messageBelongsToSomeoneElse = possibleFile.Recipient != requestorId;
            if (messageBelongsToSomeoneElse)
            {
               return new NotFoundObjectResult(new ErrorResponse(DownloadTransferPreviewError.NotFound));
            }

            var possibleUser = await UserService.ReadAsync(requestorId, cancellationToken);
            if (possibleUser != null)
            {
               recipientUsername = possibleUser.Username;
            }
         }

         string? senderUsername = null;
         string? senderAlias = null;
         if (possibleFile.Sender != Guid.Empty)
         {
            var possibleUser = await UserService.ReadAsync(possibleFile.Sender, cancellationToken);
            if (possibleUser != null)
            {
               senderUsername = possibleUser.Username;
            }

            var possibleUserProfile = await UserProfileService.ReadAsync(possibleFile.Sender, cancellationToken);
            if (possibleUserProfile != null)
            {
               senderAlias = possibleUserProfile.Alias;
            }
         }

         return new OkObjectResult(
            new DownloadTransferFilePreviewResponse(possibleFile.FileName, possibleFile.ContentType, possibleFile.Size, senderUsername, senderAlias, recipientUsername, possibleFile.X25519PublicKey, possibleFile.Created, possibleFile.Expiration));
      }

      public async Task<IActionResult> GetMessageCiphertextAsync(DownloadTransferCiphertextRequest request, Guid requestorId, CancellationToken cancellationToken)
      {
         var possibleMessage = await MessageService.ReadAsync(request.Id, cancellationToken);
         if (possibleMessage is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferCiphertextError.NotFound));
         }

         var messageBelongsToSomeoneElse = possibleMessage.Recipient != Guid.Empty && possibleMessage.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferCiphertextError.NotFound));
         }

         // Remove server-side encryption
         var serverDecryptionKey = Convert.FromBase64String(request.ServerDecryptionKeyBase64);
         var cipherTextServer = await MessageTransferItemStorageService.ReadAsync(request.Id, cancellationToken);
         var cipherTextClient = SimpleEncryptionService.Decrypt(serverDecryptionKey, possibleMessage.ServerIV, cipherTextServer);

         // Compare digests AFTER removing server-side encryption
         var cipherTextClientDigest = ItemDigestFunction(cipherTextClient);
         var digestsMatch = SimpleHashService.CompareDigests(possibleMessage.ServerDigest, cipherTextClientDigest);
         if (!digestsMatch)
         {
            return new BadRequestObjectResult(new ErrorResponse(DownloadTransferCiphertextError.ServerDecryptionFailed));
         }

         if (possibleMessage.Recipient == Guid.Empty)
         {
            await MessageService.DeleteAsync(request.Id, cancellationToken);
            MessageTransferItemStorageService.Delete(request.Id);
         }

         return new OkObjectResult(
             new DownloadTransferCiphertextResponse(Convert.ToBase64String(cipherTextClient), possibleMessage.ClientIV));
      }

      public async Task<IActionResult> GetFileCiphertextAsync(DownloadTransferCiphertextRequest request, Guid requestorId, CancellationToken cancellationToken)
      {
         var possibleFile = await FileService.ReadAsync(request.Id, cancellationToken);
         if (possibleFile is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferCiphertextError.NotFound));
         }

         var fileBelongsToSomeoneElse = possibleFile.Recipient != Guid.Empty && possibleFile.Recipient != requestorId;
         if (fileBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferCiphertextError.NotFound));
         }

         // Remove server-side encryption
         var serverDecryptionKey = Convert.FromBase64String(request.ServerDecryptionKeyBase64);
         var cipherTextServer = await FileTransferItemStorageService.ReadAsync(request.Id, cancellationToken);
         var cipherTextClient = SimpleEncryptionService.Decrypt(serverDecryptionKey, possibleFile.ServerIV, cipherTextServer);

         // Compare digests AFTER removing server-side encryption
         var cipherTextClientDigest = ItemDigestFunction(cipherTextClient);
         var digestsMatch = SimpleHashService.CompareDigests(possibleFile.ServerDigest, cipherTextClientDigest);
         if (!digestsMatch)
         {
            return new BadRequestObjectResult(new ErrorResponse(DownloadTransferCiphertextError.ServerDecryptionFailed));
         }

         if (possibleFile.Recipient == Guid.Empty)
         {
            await FileService.DeleteAsync(request.Id, cancellationToken);
            FileTransferItemStorageService.Delete(request.Id);
         }

         return new OkObjectResult(
             new DownloadTransferCiphertextResponse(Convert.ToBase64String(cipherTextClient), possibleFile.ClientIV));
      }

      public async Task<IActionResult> GetMessageSignatureAsync(DownloadTransferSignatureRequest request, Guid requestorId, CancellationToken cancellationToken)
      {
         var possibleMessage = await MessageService.ReadAsync(request.Id, cancellationToken);
         if (possibleMessage is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferSignatureError.NotFound));
         }

         var messageBelongsToSomeoneElse = possibleMessage.Recipient != Guid.Empty && possibleMessage.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferSignatureError.NotFound));
         }

         return new OkObjectResult(
            new DownloadTransferSignatureResponse(possibleMessage.Signature, possibleMessage.Ed25519PublicKey));
      }

      public async Task<IActionResult> GetFileSignatureAsync(DownloadTransferSignatureRequest request, Guid requestorId, CancellationToken cancellationToken)
      {
         var possibleFile = await FileService.ReadAsync(request.Id, cancellationToken);
         if (possibleFile is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferSignatureError.NotFound));
         }

         var messageBelongsToSomeoneElse = possibleFile.Recipient != Guid.Empty && possibleFile.Recipient != requestorId;
         if (messageBelongsToSomeoneElse)
         {
            return new NotFoundObjectResult(new ErrorResponse(DownloadTransferSignatureError.NotFound));
         }

         return new OkObjectResult(
            new DownloadTransferSignatureResponse(possibleFile.Signature, possibleFile.Ed25519PublicKey));
      }
   }
}
