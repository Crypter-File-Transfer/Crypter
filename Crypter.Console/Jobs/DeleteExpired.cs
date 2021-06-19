using Crypter.DataAccess.FileSystem;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Crypter.Console.Jobs
{
   public class DeleteExpired
   {
      private string FileStorePath { get; }
      private IBaseItemService<MessageItem> MessageService { get; }
      private IBaseItemService<FileItem> FileService { get; }
      private readonly ILogger<DeleteExpired> Logger;

      public DeleteExpired(string fileStorePath, IBaseItemService<MessageItem> messageService, IBaseItemService<FileItem> fileService, ILogger<DeleteExpired> logger)
      {
         FileStorePath = fileStorePath;
         MessageService = messageService;
         FileService = fileService;
         Logger = logger;
      }

      public async Task RunAsync()
      {
         Logger.LogInformation($"{DateTime.Now:HH:mm:ss} DeleteExpired is working.");

         var expiredMessages = await MessageService.FindExpiredAsync();
         foreach (MessageItem expiredItem in expiredMessages)
         {
            Logger.LogInformation($"Deleting message {expiredItem.Id}");
            await MessageService.DeleteAsync(expiredItem.Id);
            var DeleteItem = new FileCleanup(expiredItem.Id, FileStorePath);
            DeleteItem.CleanDirectory(false);
            Logger.LogInformation($"Delete success");
         }

         var expiredFiles = await FileService.FindExpiredAsync();
         foreach (FileItem expiredItem in expiredFiles)
         {
            Logger.LogInformation($"Deleting file {expiredItem.Id}");
            await FileService.DeleteAsync(expiredItem.Id);
            var DeleteItem = new FileCleanup(expiredItem.Id, FileStorePath);
            DeleteItem.CleanDirectory(true);
            Logger.LogInformation($"Delete success");
         }
      }
   }
}
