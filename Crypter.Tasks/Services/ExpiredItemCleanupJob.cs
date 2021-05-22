using Crypter.DataAccess.FileSystem;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Tasks.Services
{
    public class ExpiredItemCleanupJob : CronJobService
    {
        private readonly string BaseSaveDirectory;
        private readonly IBaseItemService<MessageItem> _messageService;
        private readonly IBaseItemService<FileItem> _fileService;
        private readonly ILogger<ExpiredItemCleanupJob> _logger;

        public ExpiredItemCleanupJob(
            IScheduleConfig<ExpiredItemCleanupJob> config,
            IBaseItemService<MessageItem> messageService,
            IBaseItemService<FileItem> fileService,
            ILogger<ExpiredItemCleanupJob> logger,
            IConfiguration configuration)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _messageService = messageService;
            _fileService = fileService;
            BaseSaveDirectory = configuration["EncryptedFileStore"];
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ExpiredItemCleanupJob starts.");
            return base.StartAsync(cancellationToken);
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ExpiredItemCleanupJob is working.");

            var expiredMessages = await _messageService.FindExpiredAsync();
            foreach (MessageItem expiredItem in expiredMessages)
            {
                _logger.LogInformation($"Deleting message {expiredItem.Id}");
                await _messageService.DeleteAsync(expiredItem.Id);
                FileCleanup DeleteItem = new FileCleanup(expiredItem.Id, BaseSaveDirectory);
                DeleteItem.CleanDirectory(false);
                _logger.LogInformation($"Delete success");
            }

            var expiredFiles = await _fileService.FindExpiredAsync();
            foreach (FileItem expiredItem in expiredFiles)
            {
                _logger.LogInformation($"Deleting file {expiredItem.Id}");
                await _fileService.DeleteAsync(expiredItem.Id);
                FileCleanup DeleteItem = new FileCleanup(expiredItem.Id, BaseSaveDirectory);
                DeleteItem.CleanDirectory(true);
                _logger.LogInformation($"Delete success");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ExpiredItemCleanupJob is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }

}
