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
using Crypter.Core.Entities;
using Crypter.Core.Extensions;
using Crypter.Core.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface ITransferUploadService
   {
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadAnonymousMessageAsync(UploadMessageTransferRequest request, CancellationToken cancellationToken);
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadUserMessageAsync(Maybe<Guid> sender, Maybe<string> recipientUsername, UploadMessageTransferRequest request, CancellationToken cancellationToken);
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadAnonymousFileAsync(UploadFileTransferRequest request, CancellationToken cancellationToken);
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadUserFileAsync(Maybe<Guid> sender, Maybe<string> recipientUsername, UploadFileTransferRequest request, CancellationToken cancellationToken);
   }

   public class TransferUploadService : ITransferUploadService
   {
      private readonly DataContext _context;
      private readonly IServerMetricsService _serverMetricsService;
      private readonly ITransferStorageService _transferStorageService;
      private readonly IHangfireBackgroundService _hangfireBackgroundService;
      private const int _maxLifetimeHours = 24;
      private const int _minLifetimeHours = 1;

      public TransferUploadService(DataContext context, IServerMetricsService serverMetricsService, ITransferStorageService transferStorageService, IHangfireBackgroundService hangfireBackgroundService)
      {
         _context = context;
         _serverMetricsService = serverMetricsService;
         _transferStorageService = transferStorageService;
         _hangfireBackgroundService = hangfireBackgroundService;
      }

      public Task<Either<UploadTransferError, UploadTransferResponse>> UploadAnonymousMessageAsync(UploadMessageTransferRequest request, CancellationToken cancellationToken)
      {
         return from diskSpace in GetRequiredDiskSpaceAsync(request, cancellationToken)
                from lifetimeHours in GetValidLifetimeHours(request).AsTask()
                let transferId = Guid.NewGuid()
                from savedToDisk in SaveTransferToDiskAsync(TransferItemType.Message, TransferUserType.Anonymous, transferId, request, cancellationToken).ToLeftEitherAsync(Unit.Default)
                from savedToDatabase in Either<UploadTransferError, UploadTransferResponse>.FromRightAsync(SaveAnonymousMessageTransferToDatabaseAsync(transferId, diskSpace, request, cancellationToken))
                let jobId = ScheduleTransferDeletion(transferId, TransferItemType.Message, TransferUserType.Anonymous, savedToDatabase.ExpirationUTC)
                select savedToDatabase;
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadUserMessageAsync(Maybe<Guid> sender, Maybe<string> recipientUsername, UploadMessageTransferRequest request, CancellationToken cancellationToken)
      {
         Maybe<Guid> recipientId = await recipientUsername.MatchAsync(
            () => Maybe<Guid>.None,
            async x => await GetRecipientIdAsync(sender, x, cancellationToken));

         if (recipientUsername.IsSome && recipientId.IsNone)
         {
            return UploadTransferError.RecipientNotFound;
         }

         var task = from diskSpace in GetRequiredDiskSpaceAsync(request, cancellationToken)
                    from lifetimeHours in GetValidLifetimeHours(request).AsTask()
                    let transferId = Guid.NewGuid()
                    from savedToDisk in SaveTransferToDiskAsync(TransferItemType.Message, TransferUserType.User, transferId, request, cancellationToken).ToLeftEitherAsync(Unit.Default)
                    from savedToDatabase in Either<UploadTransferError, UploadTransferResponse>.FromRightAsync(SaveUserMessageTransferToDatabaseAsync(transferId, diskSpace, sender, recipientId, request, cancellationToken))
                    from emailJobId in Either<UploadTransferError, string>.FromRightAsync(QueueTransferNotificationAsync(transferId, TransferItemType.Message, recipientId))
                    let deletionJobId = ScheduleTransferDeletion(transferId, TransferItemType.Message, TransferUserType.User, savedToDatabase.ExpirationUTC)
                    select savedToDatabase;

         return await task;
      }

      public Task<Either<UploadTransferError, UploadTransferResponse>> UploadAnonymousFileAsync(UploadFileTransferRequest request, CancellationToken cancellationToken)
      {
         return from diskSpace in GetRequiredDiskSpaceAsync(request, cancellationToken)
                from lifetimeHours in GetValidLifetimeHours(request).AsTask()
                let transferId = Guid.NewGuid()
                from savedToDisk in SaveTransferToDiskAsync(TransferItemType.File, TransferUserType.Anonymous, transferId, request, cancellationToken).ToLeftEitherAsync(Unit.Default)
                from savedToDatabase in Either<UploadTransferError, UploadTransferResponse>.FromRightAsync(SaveAnonymousFileTransferToDatabaseAsync(transferId, diskSpace, request, cancellationToken))
                let jobId = ScheduleTransferDeletion(transferId, TransferItemType.File, TransferUserType.Anonymous, savedToDatabase.ExpirationUTC)
                select savedToDatabase;
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadUserFileAsync(Maybe<Guid> sender, Maybe<string> recipientUsername, UploadFileTransferRequest request, CancellationToken cancellationToken)
      {
         Maybe<Guid> recipientId = await recipientUsername.MatchAsync(
            () => Maybe<Guid>.None,
            async x => await GetRecipientIdAsync(sender, x, cancellationToken));

         if (recipientUsername.IsSome && recipientId.IsNone)
         {
            return UploadTransferError.RecipientNotFound;
         }

         var task = from diskSpace in GetRequiredDiskSpaceAsync(request, cancellationToken)
                    from lifetimeHours in GetValidLifetimeHours(request).AsTask()
                    let transferId = Guid.NewGuid()
                    from savedToDisk in SaveTransferToDiskAsync(TransferItemType.File, TransferUserType.User, transferId, request, cancellationToken).ToLeftEitherAsync(Unit.Default)
                    from savedToDatabase in Either<UploadTransferError, UploadTransferResponse>.FromRightAsync(SaveUserFileTransferToDatabaseAsync(transferId, diskSpace, sender, recipientId, request, cancellationToken))
                    from emailJobId in Either<UploadTransferError, string>.FromRightAsync(QueueTransferNotificationAsync(transferId, TransferItemType.File, recipientId))
                    let deletionJobId = ScheduleTransferDeletion(transferId, TransferItemType.File, TransferUserType.User, savedToDatabase.ExpirationUTC)
                    select savedToDatabase;

         return await task;
      }

      private async Task<Either<UploadTransferError, int>> GetRequiredDiskSpaceAsync(IUploadTransferRequest request, CancellationToken cancellationToken)
      {
         int requiredDiskSpace = request.Box.Contents.Length + request.Box.Nonce.Length;

         bool serverHasDiskSpace = await IsDiskSpaceForTransferAsync(requiredDiskSpace, cancellationToken);
         return serverHasDiskSpace
            ? requiredDiskSpace
            : UploadTransferError.OutOfSpace;
      }

      private static Either<UploadTransferError, int> GetValidLifetimeHours(IUploadTransferRequest request)
      {
         bool inRange = request.LifetimeHours <= _maxLifetimeHours || request.LifetimeHours >= _minLifetimeHours;
         return inRange
            ? request.LifetimeHours
            : UploadTransferError.InvalidRequestedLifetimeHours;
      }

      private async Task<Maybe<Guid>> GetRecipientIdAsync(Maybe<Guid> senderId, string recipientUsername, CancellationToken cancellationToken)
      {
         Guid? effectiveSenderId = senderId.Match<Guid?>(
            () => null,
            x => x);

         Guid recipientId = await _context.Users
            .Where(x => x.Username.ToLower() == recipientUsername.ToLower())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(effectiveSenderId))
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

         return recipientId == default
            ? Maybe<Guid>.None
            : recipientId;
      }

      private async Task<Maybe<UploadTransferError>> SaveTransferToDiskAsync(TransferItemType itemType, TransferUserType userType, Guid id, IUploadTransferRequest request, CancellationToken cancellationToken)
      {
         var storageParameters = new TransferStorageParameters(id, itemType, userType, request.Box.Nonce, request.Box.Contents);
         var storageSuccess = await _transferStorageService.SaveTransferAsync(storageParameters, cancellationToken);
         return storageSuccess
            ? Maybe<UploadTransferError>.None
            : UploadTransferError.UnknownError;
      }

      private async Task<UploadTransferResponse> SaveAnonymousMessageTransferToDatabaseAsync(Guid id, int requiredDiskSpace, UploadMessageTransferRequest request, CancellationToken cancellationToken)
      {
         DateTime now = DateTime.UtcNow;
         DateTime expiration = now.AddHours(request.LifetimeHours);
         var newTransferEntity = new AnonymousMessageTransferEntity(id, requiredDiskSpace, request.PublicKey, request.ServerProof, request.CompressionType, now, expiration, request.Subject);
         _context.AnonymousMessageTransfers.Add(newTransferEntity);
         await _context.SaveChangesAsync(cancellationToken);

         return new UploadTransferResponse(id, expiration, TransferUserType.Anonymous);
      }

      private async Task<UploadTransferResponse> SaveUserMessageTransferToDatabaseAsync(Guid id, int requiredDiskSpace, Maybe<Guid> sender, Maybe<Guid> recipient, UploadMessageTransferRequest request, CancellationToken cancellationToken)
      {
         DateTime now = DateTime.UtcNow;
         DateTime expiration = now.AddHours(request.LifetimeHours);

         Guid? nullableSenderId = sender.Match<Guid?>(
            () => null,
            x => x);

         Guid? nullableRecipientId = recipient.Match<Guid?>(
            () => null,
            x => x);

         var newTransferEntity = new UserMessageTransferEntity(id, requiredDiskSpace, request.PublicKey, request.ServerProof, request.CompressionType, now, expiration, nullableSenderId, nullableRecipientId, request.Subject);
         _context.UserMessageTransfers.Add(newTransferEntity);
         await _context.SaveChangesAsync(cancellationToken);

         return new UploadTransferResponse(id, expiration, TransferUserType.User);
      }

      private async Task<UploadTransferResponse> SaveAnonymousFileTransferToDatabaseAsync(Guid id, int requiredDiskSpace, UploadFileTransferRequest request, CancellationToken cancellationToken)
      {
         DateTime now = DateTime.UtcNow;
         DateTime expiration = now.AddHours(request.LifetimeHours);
         var newTransferEntity = new AnonymousFileTransferEntity(id, requiredDiskSpace, request.PublicKey, request.ServerProof, request.CompressionType, now, expiration, request.Filename, request.ContentType);
         _context.AnonymousFileTransfers.Add(newTransferEntity);
         await _context.SaveChangesAsync(cancellationToken);

         return new UploadTransferResponse(id, expiration, TransferUserType.Anonymous);
      }

      private async Task<UploadTransferResponse> SaveUserFileTransferToDatabaseAsync(Guid id, int requiredDiskSpace, Maybe<Guid> sender, Maybe<Guid> recipient, UploadFileTransferRequest request, CancellationToken cancellationToken)
      {
         DateTime now = DateTime.UtcNow;
         DateTime expiration = now.AddHours(request.LifetimeHours);

         Guid? nullableSenderId = sender.Match<Guid?>(
            () => null,
            x => x);

         Guid? nullableRecipientId = recipient.Match<Guid?>(
            () => null,
            x => x);

         var newTransferEntity = new UserFileTransferEntity(id, requiredDiskSpace, request.PublicKey, request.ServerProof, request.CompressionType, now, expiration, nullableSenderId, nullableRecipientId, request.Filename, request.ContentType);
         _context.UserFileTransfers.Add(newTransferEntity);
         await _context.SaveChangesAsync(cancellationToken);

         return new UploadTransferResponse(id, expiration, TransferUserType.User);
      }

      private async Task<bool> IsDiskSpaceForTransferAsync(int transferSize, CancellationToken cancellationToken)
      {
         var diskMetrics = await _serverMetricsService.GetAggregateDiskMetricsAsync(cancellationToken);
         return transferSize <= diskMetrics.Available;
      }

      private async Task<string> QueueTransferNotificationAsync(Guid itemId, TransferItemType itemType, Maybe<Guid> maybeUserId)
      {
         return await maybeUserId.MatchAsync(
            () => string.Empty,
            async userId =>
            {
               UserEntity user = await _context.Users
                  .Where(x => x.Id == userId)
                  .Where(x => x.NotificationSetting.EnableTransferNotifications
                     && x.EmailVerified
                     && x.NotificationSetting.EmailNotifications)
                  .FirstOrDefaultAsync();

               return BackgroundJob.Enqueue(() => _hangfireBackgroundService.SendTransferNotificationAsync(itemId, itemType, CancellationToken.None));
            });
      }

      private string ScheduleTransferDeletion(Guid itemId, TransferItemType itemType, TransferUserType userType, DateTime itemExpiration)
         => BackgroundJob.Schedule(() => _hangfireBackgroundService.DeleteTransferAsync(itemId, itemType, userType, CancellationToken.None), itemExpiration);
   }
}
