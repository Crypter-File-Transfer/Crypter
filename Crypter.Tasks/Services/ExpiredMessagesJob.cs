using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Crypter.DataAccess;
using Crypter.DataAccess.Models;
using Crypter.DataAccess.Queries;
using Crypter.DataAccess.Helpers; 
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Crypter.Tasks.Services
{
    public class ExpiredMessagesJob : CronJobService
    {
        private readonly CrypterDB Db;
        private readonly string BaseSaveDirectory;
        private readonly ILogger<ExpiredMessagesJob> _logger;

        public ExpiredMessagesJob(IScheduleConfig<ExpiredMessagesJob> config, ILogger<ExpiredMessagesJob> logger, CrypterDB db, IConfiguration configuration)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            Db = db;
            BaseSaveDirectory = configuration["EncryptedFileStore"];
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ExpiredMessagesJob starts.");
            return base.StartAsync(cancellationToken);
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} ExpiredMessagesJob is working.");
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            //get list of expired uploads
            List<TextUploadItem> expiredListResult = await query.FindExpiredItemsAsync();
            //process each expired
            foreach (TextUploadItem expiredItem in expiredListResult)
            {
                string expiredID = expiredItem.ID;
                //delete each row from database
                await expiredItem.DeleteAsync(Db);
                _logger.LogInformation($"Deleted {expiredItem.ID}");
                //delete each file from the file system given Id
                FileCleanup DeleteItem = new FileCleanup(expiredID, BaseSaveDirectory);
                //boolean argument indicates the item is not a file
                DeleteItem.CleanExpiredDirectory(false);
            }
            await Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ExpiredMessagesJob is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }

}
