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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Metrics;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Core;
using Crypter.Core.Entities;
using Crypter.Core.Repositories;
using Crypter.Core.Services;
using Crypter.Test.Core_Tests.Models;
using Crypter.Test.Shared;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   public class TransferUploadService_Tests
   {
      private Random _random;
      private DataContext _dataContext;
      private TransferUploadService _uploadService;
      private ITransferRepository _transferStorageService;
      private IServerMetricsService _serverMetricsService;
      private DummyBackgroundJobClient _backgroundJobClient;
      private IHangfireBackgroundService _hangfireBackgroundService;

      [OneTimeSetUp]
      public void OneTimeSetUp()
      {
         _random = new Random();
         _dataContext = GenerateMockDataContext();
         _backgroundJobClient = GenerateMockBackgroundJobClient();
         _serverMetricsService = GenerateMockServerMetricsService();
         _transferStorageService = GenerateMockTransferStorageService();
         _hangfireBackgroundService = GenerateMockHangfireBackgroundService();

         // This must happen last.
         _uploadService = GenerateMockUploadService();
      }

      [Test]
      public async Task Upload_File_Transfer_Async_Null_User_Does_Not_Enqueue_Client()
      {
         Guid senderId = Guid.Empty;
         string recipientUsername = string.Empty;
         Stream mockStream = GenerateMockStream();
         UploadFileTransferRequest request = GenerateMockRequest();
         _ = await _uploadService.UploadFileTransferAsync(
            senderId,
            recipientUsername,
            request,
            mockStream
         );

         Assert.IsEmpty(_backgroundJobClient.Jobs);
      }

      private TransferUploadService GenerateMockUploadService()
      {
         var uploadService = new TransferUploadService(
            context: _dataContext,
            serverMetricsService: _serverMetricsService,
            transferStorageService: _transferStorageService,
            hangfireBackgroundService: _hangfireBackgroundService,
            backgroundJobClient: _backgroundJobClient,
            hashIdService: null
         );

         return uploadService;
      }

      private Stream GenerateMockStream()
      {
         byte[] buffer = new byte[1024];
         int scale = _random.Next(100, 1000);
         Stream mockStream = new MemoryStream(buffer);
         while (scale > 0)
         {
            int bytesToWrite = Math.Min(buffer.Length, scale);
            _random.NextBytes(buffer);
            mockStream.Write(buffer, 0, bytesToWrite);
            scale -= bytesToWrite;
         }

         return mockStream;
      }

      /// NOTE: This method is <see langword="static"/> because it doesn't access instance members.
      private static DataContext GenerateMockDataContext()
      {
         DbSet<UserEntity> users = GenerateMockUsers();
         Mock<DataContext> mockDataContext = new();
         _ = mockDataContext.Setup(context => context.Users).Returns(users);
         return mockDataContext.Object;
      }

      /// NOTE: This method is <see langword="static"/> because it doesn't access instance members.
      private static DummyBackgroundJobClient GenerateMockBackgroundJobClient() => new();

      /// NOTE: This method is <see langword="static"/> because it doesn't access instance members.
      private static IServerMetricsService GenerateMockServerMetricsService()
      {
         Mock<IServerMetricsService> mockServerMetricsService = new();
         _ = mockServerMetricsService.Setup(service =>
            service.GetAggregateDiskMetricsAsync(CancellationToken.None))
               .Returns(
                  Task.FromResult(
                     new PublicStorageMetricsResponse(
                        allocated: int.MaxValue,
                        available: int.MaxValue
                     )
                  )
               );

         return mockServerMetricsService.Object;
      }

      /// NOTE: This method is <see langword="static"/> because it doesn't access instance members.
      private static ITransferRepository GenerateMockTransferStorageService()
      {
         Mock<ITransferRepository> mockTransferStorageService = new();
         _ = mockTransferStorageService.Setup(service => service.SaveTransferAsync(
            It.IsAny<Guid>(),
            It.IsAny<TransferItemType>(),
            It.IsAny<TransferUserType>(),
            It.IsAny<Stream>()
         )).Returns(Task.FromResult(true));
         return mockTransferStorageService.Object;
      }

      /// NOTE: This method is <see langword="static"/> because it doesn't access instance members.
      private static IHangfireBackgroundService GenerateMockHangfireBackgroundService()
      {
         Mock<IHangfireBackgroundService> mockHangfireBackgroundService = new();
         _ = mockHangfireBackgroundService.Setup(service => service.SendTransferNotificationAsync(
            It.IsAny<Guid>(),
            It.IsAny<TransferItemType>()
         )).Returns(Task.CompletedTask);
         return mockHangfireBackgroundService.Object;
      }

      /// NOTE: This method is <see langword="static"/> because it doesn't access instance members.
      private static DbSet<UserEntity> GenerateMockUsers()
      {
         IEnumerable<UserEntity> users = GenerateUsers();
         IQueryable<UserEntity> usersQueryable = users.AsQueryable();

         Mock<DbSet<UserEntity>> usersDbSet = new();

         _ = usersDbSet.As<IAsyncEnumerable<UserEntity>>()
             .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
             .Returns(new TestAsyncEnumerator<UserEntity>(usersQueryable.GetEnumerator()));

         _ = usersDbSet.As<IQueryable<UserEntity>>()
             .Setup(m => m.Provider)
             .Returns(new TestAsyncQueryProvider<UserEntity>(usersQueryable.Provider));

         _ = usersDbSet.As<IQueryable<UserEntity>>().Setup(m => m.Expression).Returns(usersQueryable.Expression);
         _ = usersDbSet.As<IQueryable<UserEntity>>().Setup(m => m.ElementType).Returns(usersQueryable.ElementType);
         _ = usersDbSet.As<IQueryable<UserEntity>>().Setup(m => m.GetEnumerator()).Returns(() => usersQueryable.GetEnumerator());
         return usersDbSet.Object;
      }

      /// NOTE: This method is <see langword="static"/> because it doesn't access instance members.
      private static IEnumerable<UserEntity> GenerateUsers() => Enumerable.Range(0, 10).Select(
         index => new UserEntity(
            id: Guid.NewGuid(),
            username: $"user{index}",
            emailAddress: $"user{index}@example.com",
            passwordHash: null,
            passwordSalt: null,
            serverPasswordVersion: 1,
            clientPasswordVersion: 1,
            emailVerified: true,
            created: DateTime.Now.AddDays(-index),
            lastLogin: DateTime.Now.AddMinutes(-index)
         )
      );

      private static UploadFileTransferRequest GenerateMockRequest()
      {
         UploadFileTransferRequest request = new(
            fileName: "sample.txt",
            "text/plain",
            publicKey: null,
            keyExchangeNonce: null,
            proof: null,
            lifetimeHours: 3
         );

         return request;
      }
   }
}
