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
using Crypter.Core.Entities;
using Crypter.Core.Extensions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface ITransferUploadService
   {
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(Maybe<Guid> senderId, Maybe<string> recipientUsername, UploadFileTransferRequest request, Stream ciphertextStream);
      Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(Maybe<Guid> senderId, Maybe<string> recipientUsername, UploadMessageTransferRequest request, Stream ciphertextStream);
   }

   public class TransferUploadService : ITransferUploadService
   {
      private readonly DataContext _context;
      private readonly IServerMetricsService _serverMetricsService;
      private readonly ITransferStorageService _transferStorageService;
      private readonly IHangfireBackgroundService _hangfireBackgroundService;
      private readonly IBackgroundJobClient _backgroundJobClient;
      private readonly IHashIdService _hashIdService;
      private const int _maxLifetimeHours = 24;
      private const int _minLifetimeHours = 1;

      public TransferUploadService(DataContext context, IServerMetricsService serverMetricsService, ITransferStorageService transferStorageService, IHangfireBackgroundService hangfireBackgroundService, IBackgroundJobClient backgroundJobClient, IHashIdService hashIdService)
      {
         _context = context;
         _serverMetricsService = serverMetricsService;
         _transferStorageService = transferStorageService;
         _hangfireBackgroundService = hangfireBackgroundService;
         _backgroundJobClient = backgroundJobClient;
         _hashIdService = hashIdService;
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadFileTransferAsync(Maybe<Guid> senderId, Maybe<string> recipientUsername, UploadFileTransferRequest request, Stream ciphertextStream)
      {
         Maybe<Guid> recipientId = await recipientUsername.MatchAsync(
            () => Maybe<Guid>.None,
            async x => await GetRecipientIdAsync(senderId, x));

         if (recipientUsername.IsSome && recipientId.IsNone)
         {
            return UploadTransferError.RecipientNotFound;
         }

         return await (from diskSpace in GetRequiredDiskSpaceAsync(ciphertextStream.Length)
                       from lifetimeHours in ValidateLifetimeHours(request.LifetimeHours).AsTask()
                       let transferId = Guid.NewGuid()
                       let transferUserType = DetermineTransferUserType(senderId, recipientId)
                       from savedToDisk in SaveFileToDiskAsync(transferUserType, transferId, ciphertextStream).ToLeftEitherAsync(Unit.Default)
                       from savedToDatabase in Either<UploadTransferError, UploadTransferResponse>.FromRightAsync(SaveFileTransferToDatabaseAsync(transferId, senderId, recipientId, diskSpace, request))
                       from _ in Either<UploadTransferError, Unit>.FromRightAsync(QueueTransferNotificationAsync(transferId, TransferItemType.File, recipientId))
                       let jobId = ScheduleTransferDeletion(transferId, TransferItemType.File, transferUserType, savedToDatabase.ExpirationUTC)
                       select savedToDatabase);
      }

      public async Task<Either<UploadTransferError, UploadTransferResponse>> UploadMessageTransferAsync(Maybe<Guid> senderId, Maybe<string> recipientUsername, UploadMessageTransferRequest request, Stream ciphertextStream)
      {
         Maybe<Guid> recipientId = await recipientUsername.MatchAsync(
            () => Maybe<Guid>.None,
            async x => await GetRecipientIdAsync(senderId, x));

         if (recipientUsername.IsSome && recipientId.IsNone)
         {
            return UploadTransferError.RecipientNotFound;
         }

         var task = from diskSpace in GetRequiredDiskSpaceAsync(ciphertextStream.Length)
                    from lifetimeHours in ValidateLifetimeHours(request.LifetimeHours).AsTask()
                    let transferId = Guid.NewGuid()
                    let transferUserType = DetermineTransferUserType(senderId, recipientId)
                    from savedToDisk in SaveMessageToDiskAsync(transferUserType, transferId, ciphertextStream).ToLeftEitherAsync(Unit.Default)
                    from savedToDatabase in Either<UploadTransferError, UploadTransferResponse>.FromRightAsync(SaveMessageTransferToDatabaseAsync(transferId, senderId, recipientId, diskSpace, request))
                    from _ in Either<UploadTransferError, Unit>.FromRightAsync(QueueTransferNotificationAsync(transferId, TransferItemType.Message, recipientId))
                    let deletionJobId = ScheduleTransferDeletion(transferId, TransferItemType.Message, transferUserType, savedToDatabase.ExpirationUTC)
                    select savedToDatabase;

         return await task;
      }

      private static TransferUserType DetermineTransferUserType(Maybe<Guid> senderId, Maybe<Guid> recipientId)
      {
         if (senderId.SomeOrDefault(Guid.Empty) != Guid.Empty)
         {
            return TransferUserType.User;
         }

         if (recipientId.SomeOrDefault(Guid.Empty) != Guid.Empty)
         {
            return TransferUserType.User;
         }

         return TransferUserType.Anonymous;
      }

      private static TransferUserType DetermineTransferUserType(Guid? senderId, Guid? recipientId)
      {
         return senderId is null && recipientId is null
            ? TransferUserType.Anonymous
            : TransferUserType.User;
      }

      private async Task<UploadTransferResponse> SaveFileTransferToDatabaseAsync(Guid transferId, Maybe<Guid> senderId, Maybe<Guid> recipientId, long requiredDiskSpace, UploadFileTransferRequest request)
      {
         DateTime now = DateTime.UtcNow;
         DateTime expiration = now.AddHours(request.LifetimeHours);

         Guid? nullableSenderId = senderId.Bind<Guid?>(x => x).SomeOrDefault(null);
         Guid? nullableRecipientId = recipientId.Bind<Guid?>(x => x).SomeOrDefault(null);

         if (nullableRecipientId is null && nullableRecipientId is null)
         {
            AnonymousFileTransferEntity transferEntity = new AnonymousFileTransferEntity(
               id: transferId,
               size: requiredDiskSpace,
               publicKey: request.PublicKey,
               keyExchangeNonce: request.KeyExchangeNonce,
               proof: request.Proof,
               created: now,
               expiration: expiration,
               fileName: request.Filename,
               contentType: request.ContentType);
            _context.AnonymousFileTransfers.Add(transferEntity);
         }
         else
         {
            UserFileTransferEntity transferEntity = new UserFileTransferEntity(
               id: transferId,
               size: requiredDiskSpace,
               publicKey: request.PublicKey,
               keyExchangeNonce: request.KeyExchangeNonce,
               proof: request.Proof,
               created: now,
               expiration: expiration,
               senderId: nullableSenderId,
               recipientId: nullableRecipientId,
               fileName: request.Filename,
               contentType: request.ContentType);
            _context.UserFileTransfers.Add(transferEntity);
         }

         await _context.SaveChangesAsync();

         string hashId = _hashIdService.Encode(transferId);
         TransferUserType userType = DetermineTransferUserType(nullableSenderId, nullableRecipientId);
         return new UploadTransferResponse(hashId, expiration, userType);
      }

      private async Task<UploadTransferResponse> SaveMessageTransferToDatabaseAsync(Guid transferId, Maybe<Guid> senderId, Maybe<Guid> recipientId, long requiredDiskSpace, UploadMessageTransferRequest request)
      {
         DateTime now = DateTime.UtcNow;
         DateTime expiration = now.AddHours(request.LifetimeHours);

         Guid? nullableSenderId = senderId.Bind<Guid?>(x => x).SomeOrDefault(null);
         Guid? nullableRecipientId = recipientId.Bind<Guid?>(x => x).SomeOrDefault(null);

         if (nullableRecipientId is null && nullableRecipientId is null)
         {
            AnonymousMessageTransferEntity transferEntity = new AnonymousMessageTransferEntity(
               id: transferId,
               size: requiredDiskSpace,
               publicKey: request.PublicKey,
               keyExchangeNonce: request.KeyExchangeNonce,
               proof: request.Proof,
               created: now,
               expiration: expiration,
               subject: request.Subject);
            _context.AnonymousMessageTransfers.Add(transferEntity);
         }
         else
         {
            UserMessageTransferEntity transferEntity = new UserMessageTransferEntity(
               id: transferId,
               size: requiredDiskSpace,
               publicKey: request.PublicKey,
               keyExchangeNonce: request.KeyExchangeNonce,
               proof: request.Proof,
               created: now,
               expiration: expiration,
               senderId: nullableSenderId,
               recipientId: nullableRecipientId,
               subject: request.Subject);
            _context.UserMessageTransfers.Add(transferEntity);
         }

         await _context.SaveChangesAsync();

         string hashId = _hashIdService.Encode(transferId);
         TransferUserType userType = DetermineTransferUserType(nullableSenderId, nullableRecipientId);
         return new UploadTransferResponse(hashId, expiration, userType);
      }

      private async Task<Either<UploadTransferError, long>> GetRequiredDiskSpaceAsync(long ciphertextSize, CancellationToken cancellationToken = default)
      {
         bool serverHasDiskSpace = await IsDiskSpaceForTransferAsync(ciphertextSize, cancellationToken);
         return serverHasDiskSpace
            ? ciphertextSize
            : UploadTransferError.OutOfSpace;
      }

      private static Either<UploadTransferError, int> ValidateLifetimeHours(int lifetimeHours)
      {
         bool inRange = lifetimeHours <= _maxLifetimeHours || lifetimeHours >= _minLifetimeHours;
         return inRange
            ? lifetimeHours
            : UploadTransferError.InvalidRequestedLifetimeHours;
      }

      private async Task<Maybe<Guid>> GetRecipientIdAsync(Maybe<Guid> senderId, string recipientUsername, CancellationToken cancellationToken = default)
      {
         Guid? nullableSenderId = senderId.Bind<Guid?>(x => x).SomeOrDefault(null);

         Guid recipientId = await _context.Users
            .Where(x => x.Username.ToLower() == recipientUsername.ToLower())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(nullableSenderId))
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

         return recipientId == default
            ? Maybe<Guid>.None
            : recipientId;
      }

      private async Task<Maybe<UploadTransferError>> SaveMessageToDiskAsync(TransferUserType userType, Guid id, Stream ciphertext, CancellationToken cancellationToken = default)
      {
         var storageSuccess = await _transferStorageService.SaveTransferAsync(id, TransferItemType.Message, userType, ciphertext, cancellationToken);
         return storageSuccess
            ? Maybe<UploadTransferError>.None
            : UploadTransferError.UnknownError;
      }

      private async Task<Maybe<UploadTransferError>> SaveFileToDiskAsync(TransferUserType userType, Guid id, Stream ciphertext, CancellationToken cancellationToken = default)
      {
         var storageSuccess = await _transferStorageService.SaveTransferAsync(id, TransferItemType.File, userType, ciphertext, cancellationToken);
         return storageSuccess
            ? Maybe<UploadTransferError>.None
            : UploadTransferError.UnknownError;
      }

      private async Task<bool> IsDiskSpaceForTransferAsync(long transferSize, CancellationToken cancellationToken)
      {
         var diskMetrics = await _serverMetricsService.GetAggregateDiskMetricsAsync(cancellationToken);
         return transferSize <= diskMetrics.Available;
      }

      private async Task<Unit> QueueTransferNotificationAsync(Guid itemId, TransferItemType itemType, Maybe<Guid> maybeUserId)
      {
         await maybeUserId.IfSomeAsync(
            async userId =>
            {
               UserEntity user = await _context.Users
                  .Where(x => x.Id == userId)
                  .Where(x => x.NotificationSetting.EnableTransferNotifications
                     && x.EmailVerified
                     && x.NotificationSetting.EmailNotifications)
                  .FirstOrDefaultAsync();

               _backgroundJobClient.Enqueue(() => _hangfireBackgroundService.SendTransferNotificationAsync(itemId, itemType, CancellationToken.None));
            });
         return Unit.Default;
      }

      private string ScheduleTransferDeletion(Guid itemId, TransferItemType itemType, TransferUserType userType, DateTime itemExpiration)
         => _backgroundJobClient.Schedule(() => _hangfireBackgroundService.DeleteTransferAsync(itemId, itemType, userType, true, CancellationToken.None), itemExpiration);
   }
}
