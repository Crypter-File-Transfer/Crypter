using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Crypter.Console.Jobs
{
   internal class DeleteExpired
   {
      private string FileStorePath { get; }
      private IBaseTransferService<MessageTransfer> MessageService { get; }
      private IBaseTransferService<FileTransfer> FileService { get; }
      private readonly ILogger<DeleteExpired> Logger;

      public DeleteExpired(string fileStorePath, IBaseTransferService<MessageTransfer> messageService, IBaseTransferService<FileTransfer> fileService, ILogger<DeleteExpired> logger)
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
         var expiredMessages = await MessageService.FindExpiredAsync();
         foreach (MessageTransfer expiredItem in expiredMessages)
         {
            Logger.LogInformation($"Deleting message {expiredItem.Id}");
            await MessageService.DeleteAsync(expiredItem.Id);
            messageStorageService.Delete(expiredItem.Id);
            Logger.LogInformation($"Delete complete");
         }

         var fileStorageService = new TransferItemStorageService(FileStorePath, TransferItemType.File);
         var expiredFiles = await FileService.FindExpiredAsync();
         foreach (FileTransfer expiredItem in expiredFiles)
         {
            Logger.LogInformation($"Deleting file {expiredItem.Id}");
            await FileService.DeleteAsync(expiredItem.Id);
            fileStorageService.Delete(expiredItem.Id);
            Logger.LogInformation($"Delete complete");
         }
      }
   }
}
