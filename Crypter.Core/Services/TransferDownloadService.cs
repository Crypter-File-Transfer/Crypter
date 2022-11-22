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
using Crypter.Common.Monads;
using Crypter.Contracts.Features.Transfer;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface ITransferDownloadService
   {
      Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> GetAnonymousMessagePreviewAsync(string hashId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> GetAnonymousFilePreviewAsync(string hashId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetAnonymousMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, bool deleteOnRead, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetAnonymousFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, bool deleteOnRead, CancellationToken cancellationToken);

      Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> GetUserMessagePreviewAsync(string hashId, Maybe<Guid> userId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> GetUserFilePreviewAsync(string hashId, Maybe<Guid> userId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetUserMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, Maybe<Guid> userId, CancellationToken cancellationToken);
      Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetUserFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, Maybe<Guid> userId, CancellationToken cancellationToken);
   }

   public class TransferDownloadService : ITransferDownloadService
   {
      private readonly DataContext _context;
      private readonly ITransferStorageService _transferStorageService;
      private readonly IHangfireBackgroundService _hangfireBackgroundService;
      private readonly IHashIdService _hashIdService;

      public TransferDownloadService(DataContext context, ITransferStorageService transferStorageService, IHangfireBackgroundService hangfireBackgroundService, IHashIdService hashIdService)
      {
         _context = context;
         _transferStorageService = transferStorageService;
         _hangfireBackgroundService = hangfireBackgroundService;
         _hashIdService = hashIdService;
      }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> GetAnonymousMessagePreviewAsync(string hashId, CancellationToken cancellationToken)
      {
         Guid id = _hashIdService.Decode(hashId);
         var messagePreview = await _context.AnonymousMessageTransfers
            .Where(x => x.Id == id)
            .Select(x => new DownloadTransferMessagePreviewResponse(x.Subject, x.Size, string.Empty, string.Empty, string.Empty, x.DiffieHellmanPublicKey, x.Created, x.Expiration))
            .FirstOrDefaultAsync(cancellationToken);

         if (messagePreview is null)
         {
            return DownloadTransferPreviewError.NotFound;
         }

         return messagePreview;
      }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> GetAnonymousFilePreviewAsync(string hashId, CancellationToken cancellationToken)
      {
         Guid id = _hashIdService.Decode(hashId);
         var filePreview = await _context.AnonymousFileTransfers
            .Where(x => x.Id == id)
            .Select(x => new DownloadTransferFilePreviewResponse(x.FileName, x.ContentType, x.Size, string.Empty, string.Empty, string.Empty, x.DiffieHellmanPublicKey, x.Created, x.Expiration))
            .FirstOrDefaultAsync(cancellationToken);

         if (filePreview is null)
         {
            return DownloadTransferPreviewError.NotFound;
         }

         return filePreview;
      }

      public async Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetAnonymousMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, bool deleteOnRead, CancellationToken cancellationToken)
      {
         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.AnonymousMessageTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientProof == request.RecipientProof)
            .Select(x => new { x.CompressionType })
            .FirstOrDefaultAsync(cancellationToken);

         if (databaseData is null)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         var ciphertextData = await _transferStorageService.ReadTransferAsync(id, TransferItemType.Message, TransferUserType.Anonymous, cancellationToken);

         if (deleteOnRead)
         {
            ciphertextData.IfSome(x => BackgroundJob.Enqueue(() => _hangfireBackgroundService.DeleteTransferAsync(x.Id, x.ItemType, x.UserType, CancellationToken.None)));
         }

         return ciphertextData.Match<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>>(
            () => DownloadTransferCiphertextError.NotFound,
            x => new DownloadTransferCiphertextResponse(x.Ciphertext, x.InitializationVector, databaseData.CompressionType));
      }

      public async Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetAnonymousFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, bool deleteOnRead, CancellationToken cancellationToken)
      {
         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.AnonymousFileTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientProof == request.RecipientProof)
            .Select(x => new { x.CompressionType })
            .FirstOrDefaultAsync(cancellationToken);

         if (databaseData is null)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         var ciphertextData = await _transferStorageService.ReadTransferAsync(id, TransferItemType.File, TransferUserType.Anonymous, cancellationToken);

         if (deleteOnRead)
         {
            ciphertextData.IfSome(x => BackgroundJob.Enqueue(() => _hangfireBackgroundService.DeleteTransferAsync(x.Id, x.ItemType, x.UserType, CancellationToken.None)));
         }

         return ciphertextData.Match<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>>(
            () => DownloadTransferCiphertextError.NotFound,
            x => new DownloadTransferCiphertextResponse(x.Ciphertext, x.InitializationVector, databaseData.CompressionType));
      }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> GetUserMessagePreviewAsync(string hashId, Maybe<Guid> userId, CancellationToken cancellationToken)
      {
         Guid? nullableUserId = userId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);
         var messagePreview = await _context.UserMessageTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableUserId)
            .Select(x => new DownloadTransferMessagePreviewResponse(x.Subject, x.Size, x.Sender.Username, x.Sender.Profile.Alias, x.Recipient.Username, x.DiffieHellmanPublicKey, x.Created, x.Expiration))
            .FirstOrDefaultAsync(cancellationToken);

         if (messagePreview is null)
         {
            return DownloadTransferPreviewError.NotFound;
         }

         return messagePreview;
      }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> GetUserFilePreviewAsync(string hashId, Maybe<Guid> userId, CancellationToken cancellationToken)
      {
         Guid? nullableUserId = userId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);
         var filePreview = await _context.UserFileTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableUserId)
            .Select(x => new DownloadTransferFilePreviewResponse(x.FileName, x.ContentType, x.Size, x.Sender.Username, x.Sender.Profile.Alias, x.Recipient.Username, x.DiffieHellmanPublicKey, x.Created, x.Expiration))
            .FirstOrDefaultAsync(cancellationToken);

         if (filePreview is null)
         {
            return DownloadTransferPreviewError.NotFound;
         }

         return filePreview;
      }

      public async Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetUserMessageCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, Maybe<Guid> userId, CancellationToken cancellationToken)
      {
         Guid? nullableUserId = userId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.UserMessageTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableUserId)
            .Select(x => new { x.CompressionType, x.RecipientProof })
            .FirstOrDefaultAsync(cancellationToken);

         if (databaseData is null)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         if (databaseData.RecipientProof != request.RecipientProof)
         {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
         }

         var ciphertextData = await _transferStorageService.ReadTransferAsync(id, TransferItemType.Message, TransferUserType.User, cancellationToken);
         return ciphertextData.Match<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>>(
            () => DownloadTransferCiphertextError.NotFound,
            x => new DownloadTransferCiphertextResponse(x.Ciphertext, x.InitializationVector, databaseData.CompressionType));
      }

      public async Task<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>> GetUserFileCiphertextAsync(string hashId, DownloadTransferCiphertextRequest request, Maybe<Guid> userId, CancellationToken cancellationToken)
      {
         Guid? nullableUserId = userId.Match<Guid?>(
            () => null,
            x => x);

         Guid id = _hashIdService.Decode(hashId);
         var databaseData = await _context.UserFileTransfers
            .Where(x => x.Id == id)
            .Where(x => x.RecipientId == null || x.RecipientId == nullableUserId)
            .Select(x => new { x.CompressionType, x.RecipientProof })
            .FirstOrDefaultAsync(cancellationToken);

         if (databaseData is null)
         {
            return DownloadTransferCiphertextError.NotFound;
         }

         if (databaseData.RecipientProof != request.RecipientProof)
         {
            return DownloadTransferCiphertextError.InvalidRecipientProof;
         }

         var ciphertextData = await _transferStorageService.ReadTransferAsync(id, TransferItemType.File, TransferUserType.User, cancellationToken);
         return ciphertextData.Match<Either<DownloadTransferCiphertextError, DownloadTransferCiphertextResponse>>(
            () => DownloadTransferCiphertextError.NotFound,
            x => new DownloadTransferCiphertextResponse(x.Ciphertext, x.InitializationVector, databaseData.CompressionType));
      }
   }
}
