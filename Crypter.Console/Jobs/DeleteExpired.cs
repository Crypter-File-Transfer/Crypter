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

using Crypter.Contracts.Common.Enum;
using Crypter.Core.Interfaces;
using Crypter.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Crypter.Console.Jobs
{
   internal class DeleteExpired
   {
      private string FileStorePath { get; }
      private IBaseTransferService<IMessageTransferItem> MessageService { get; }
      private IBaseTransferService<IFileTransferItem> FileService { get; }
      private readonly ILogger<DeleteExpired> Logger;

      public DeleteExpired(string fileStorePath, IBaseTransferService<IMessageTransferItem> messageService, IBaseTransferService<IFileTransferItem> fileService, ILogger<DeleteExpired> logger)
      {
         FileStorePath = fileStorePath;
         MessageService = messageService;
         FileService = fileService;
         Logger = logger;
      }

      public async Task RunAsync()
      {
         Logger.LogInformation($"{DateTime.Now:HH:mm:ss} DeleteExpired is working.");

         var messageStorageService = new TransferItemStorageService(FileStorePath, TransferItemType.Message);
         var expiredMessages = await MessageService.FindExpiredAsync(default);
         foreach (var expiredItem in expiredMessages)
         {
            Logger.LogInformation($"Deleting message {expiredItem.Id}");
            await MessageService.DeleteAsync(expiredItem.Id, default);
            messageStorageService.Delete(expiredItem.Id);
            Logger.LogInformation($"Delete complete");
         }

         var fileStorageService = new TransferItemStorageService(FileStorePath, TransferItemType.File);
         var expiredFiles = await FileService.FindExpiredAsync(default);
         foreach (var expiredItem in expiredFiles)
         {
            Logger.LogInformation($"Deleting file {expiredItem.Id}");
            await FileService.DeleteAsync(expiredItem.Id, default);
            fileStorageService.Delete(expiredItem.Id);
            Logger.LogInformation($"Delete complete");
         }
      }
   }
}
