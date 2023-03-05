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

using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Crypto.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface ITransferDownloadService
   {
      Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> GetAnonymousMessagePreviewAsync(string hashId, CancellationToken cancellationToken);
      Task<Either<TransferPreviewError, FileTransferPreviewResponse>> GetAnonymousFilePreviewAsync(string hashId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, FileStream>> GetAnonymousMessageCiphertextAsync(string hashId, byte[] proof, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, FileStream>> GetAnonymousFileCiphertextAsync(string hashId, byte[] proof);

      Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> GetUserMessagePreviewAsync(string hashId, Maybe<Guid> requestorId, CancellationToken cancellationToken);
      Task<Either<TransferPreviewError, FileTransferPreviewResponse>> GetUserFilePreviewAsync(string hashId, Maybe<Guid> requestorId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, FileStream>> GetUserMessageCiphertextAsync(string hashId, byte[] proof, Maybe<Guid> requestorId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, FileStream>> GetUserFileCiphertextAsync(string hashId, byte[] proof, Maybe<Guid> requestorId);
   }

   public class TransferDownloadService : ITransferDownloadService
   {
      private readonly DataContext _context;
      private readonly ITransferStorageService _transferStorageService;
      private readonly IHangfireBackgroundService _hangfireBackgroundService;
      private readonly IBackgroundJobClient _backgroundJobClient;
      private readonly IHashIdService _hashIdService;
      private readonly ICryptoProvider _cryptoProvider;

      public TransferDownloadService(DataContext context, ITransferStorageService transferStorageService, IHangfireBackgroundService hangfireBackgroundService, IBackgroundJobClient backgroundJobClient, IHashIdService hashIdService, ICryptoProvider cryptoProvider)
      {
         _context = context;
         _transferStorageService = transferStorageService;
         _hangfireBackgroundService = hangfireBackgroundService;
         _backgroundJobClient = backgroundJobClient;
         _hashIdService = hashIdService;
         _cryptoProvider = cryptoProvider;
      }

      public async Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> GetAnonymousMessagePreviewAsync(string hashId, CancellationToken cancellationToken)
      {
         Guid id = _hashIdService.Decode(hashId);
         var messagePreview = await _context.AnonymousMessageTransfers
            .Where(x => x.Id == id)
            .Select(x => new MessageTransferPreviewResponse(x.Subject, x.Size, string.Empty, string.Empty, string.Empty, x.PublicKey, x.KeyExchangeNonce, x.Created, x.Expiration))
            .FirstOrDefaultAsync(cancellationToken);

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.Message, TransferUserType.Anonymous);
         return messagePreview is not null && ciphertextExists
            ? messagePreview
            : TransferPreviewError.NotFound;
      }

      public async Task<Either<TransferPreviewError, FileTransferPreviewResponse>> GetAnonymousFilePreviewAsync(string hashId, CancellationToken cancellationToken)
      {
         Guid id = _hashIdService.Decode(hashId);
         var filePreview = await _context.AnonymousFileTransfers
            .Where(x => x.Id == id)
            .Select(x => new FileTransferPreviewResponse(x.FileName, x.ContentType, x.Size, string.Empty, string.Empty, string.Empty, x.PublicKey, x.KeyExchangeNonce, x.Created, x.Expiration))
            .FirstOrDefaultAsync(cancellationToken);

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.File, TransferUserType.Anonymous);
         return filePreview is not null && ciphertextExists
            ? filePreview
            : TransferPreviewError.NotFound;
      }

      public async Task<Either<DownloadTransferCiphertextError, FileStream>> GetAnonymousMessageCiphertextAsync(string hashId, byte[] proof, CancellationToken cancellationToken)
      {
         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.AnonymousMessageTransfers
            .Where(x => x.Id == id)
            .Select(x => new { x.Proof })
            .FirstOrDefaultAsync(cancellationToken);

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.Message, TransferUserType.Anonymous);
         if (databaseData is null || !ciphertextExists)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         if (!_cryptoProvider.ConstantTime.Equals(databaseData.Proof, proof))
         {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
         }

         Maybe<FileStream> ciphertextStream = _transferStorageService.GetTransfer(id, TransferItemType.Message, TransferUserType.Anonymous, true);
         ciphertextStream.IfSome(_ => QueueTransferForDeletion(id, TransferItemType.Message, TransferUserType.Anonymous));
         return ciphertextStream.ToEither(DownloadTransferCiphertextError.NotFound);
      }

      public async Task<Either<DownloadTransferCiphertextError, FileStream>> GetAnonymousFileCiphertextAsync(string hashId, byte[] proof)
      {
         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.AnonymousFileTransfers
            .Where(x => x.Id == id)
            .Select(x => new { x.Proof })
            .FirstOrDefaultAsync();

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.File, TransferUserType.Anonymous);
         if (databaseData is null || !ciphertextExists)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         if (!_cryptoProvider.ConstantTime.Equals(databaseData.Proof, proof))
         {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
         }

         Maybe<FileStream> ciphertextStream = _transferStorageService.GetTransfer(id, TransferItemType.File, TransferUserType.Anonymous, true);
         ciphertextStream.IfSome(_ => QueueTransferForDeletion(id, TransferItemType.File, TransferUserType.Anonymous));
         return ciphertextStream.ToEither(DownloadTransferCiphertextError.NotFound);
      }

      public async Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> GetUserMessagePreviewAsync(string hashId, Maybe<Guid> requestorId, CancellationToken cancellationToken)
      {
         Guid? nullableRequestorUserId = requestorId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);

         MessageTransferPreviewResponse messagePreview = await _context.UserMessageTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableRequestorUserId)
            .Select(x => new MessageTransferPreviewResponse(
               x.Subject,
               x.Size,
               x.Sender.Username,
               x.Sender.Profile.Alias,
               x.Recipient.Username,
               x.SenderId == null
                  ? x.PublicKey
                  : x.Sender.KeyPair.PublicKey,
               x.KeyExchangeNonce,
               x.Created,
               x.Expiration)).FirstOrDefaultAsync(cancellationToken);

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.Message, TransferUserType.User);
         return messagePreview is not null && ciphertextExists
            ? messagePreview
            : TransferPreviewError.NotFound;
      }

      public async Task<Either<TransferPreviewError, FileTransferPreviewResponse>> GetUserFilePreviewAsync(string hashId, Maybe<Guid> requestorId, CancellationToken cancellationToken)
      {
         Guid? nullableRequestorUserId = requestorId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);

         FileTransferPreviewResponse filePreview = await _context.UserFileTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableRequestorUserId)
            .Select(x => new FileTransferPreviewResponse(
               x.FileName,
               x.ContentType,
               x.Size,
               x.Sender.Username,
               x.Sender.Profile.Alias,
               x.Recipient.Username,
               x.SenderId == null
                  ? x.PublicKey
                  : x.Sender.KeyPair.PublicKey,
               x.KeyExchangeNonce,
               x.Created,
               x.Expiration)).FirstOrDefaultAsync(cancellationToken);

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.File, TransferUserType.User);
         return filePreview is not null && ciphertextExists
            ? filePreview
            : TransferPreviewError.NotFound;
      }

      public async Task<Either<DownloadTransferCiphertextError, FileStream>> GetUserMessageCiphertextAsync(string hashId, byte[] proof, Maybe<Guid> requestorId, CancellationToken cancellationToken)
      {
         Guid? nullableRequestorUserId = requestorId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.UserMessageTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableRequestorUserId)
            .Select(x => new { x.RecipientId, x.Proof })
            .FirstOrDefaultAsync(cancellationToken);

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.Message, TransferUserType.User);
         if (databaseData is null || !ciphertextExists)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         if (!_cryptoProvider.ConstantTime.Equals(databaseData.Proof, proof))
         {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
         }

         bool deleteOnReadCompletion = !databaseData.RecipientId.HasValue;
         Maybe<FileStream> ciphertextStream = _transferStorageService.GetTransfer(id, TransferItemType.Message, TransferUserType.User, deleteOnReadCompletion);
         ciphertextStream.IfSome(_ =>
         {
            if (deleteOnReadCompletion)
            {
               QueueTransferForDeletion(id, TransferItemType.Message, TransferUserType.User);
            }
         });
         return ciphertextStream.ToEither(DownloadTransferCiphertextError.NotFound);
      }

      public async Task<Either<DownloadTransferCiphertextError, FileStream>> GetUserFileCiphertextAsync(string hashId, byte[] proof, Maybe<Guid> requestorId)
      {
         Guid? nullableRequestorUserId = requestorId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.UserFileTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableRequestorUserId)
            .Select(x => new { x.RecipientId, x.Proof })
            .FirstOrDefaultAsync();

         bool ciphertextExists = _transferStorageService.TransferExists(id, TransferItemType.File, TransferUserType.User);
         if (databaseData is null || !ciphertextExists)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         if (!_cryptoProvider.ConstantTime.Equals(databaseData.Proof, proof))
         {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
         }

         bool deleteOnReadCompletion = !databaseData.RecipientId.HasValue;
         Maybe<FileStream> ciphertextStream = _transferStorageService.GetTransfer(id, TransferItemType.File, TransferUserType.User, deleteOnReadCompletion);
         ciphertextStream.IfSome(_ =>
         {
            if (deleteOnReadCompletion)
            {
               QueueTransferForDeletion(id, TransferItemType.File, TransferUserType.User);
            }
         });
         return ciphertextStream.ToEither(DownloadTransferCiphertextError.NotFound);
      }

      private void QueueTransferForDeletion(Guid itemId, TransferItemType itemType, TransferUserType userType)
         => _backgroundJobClient.Enqueue(() => _hangfireBackgroundService.DeleteTransferAsync(itemId, itemType, userType, false, CancellationToken.None));
   }
}
