using Crypter.Console.Jobs;
using Crypter.Core;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.Core.Services.DataAccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Crypter.Console
{
   public class Program
   {
      public static async Task<int> Main(string[] args)
      {
         IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .Build();

         var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddSingleton(configuration)
            .AddSingleton<DataContext>()
            .AddSingleton<IBaseTransferService<MessageTransfer>, MessageTransferItemService>()
            .AddSingleton<IBaseTransferService<FileTransfer>, FileTransferItemService>()
            .BuildServiceProvider();

         if (args == null || args.Length == 0 || HelpRequired(args[0]))
         {
            Help.DisplayHelp();
            return 0;
         }

         if (RequestDeleteExpired(args[0]))
         {
            var deleteJob = new DeleteExpired(configuration["EncryptedFileStore"],
               serviceProvider.GetService<IBaseTransferService<MessageTransfer>>(),
               serviceProvider.GetService<IBaseTransferService<FileTransfer>>(),
               serviceProvider.GetService<ILogger<DeleteExpired>>());

            await deleteJob.RunAsync();
            return 0;
         }

         Help.DisplayHelp();
         return -1;
      }

      private static bool HelpRequired(string param)
      {
         return param == "-h" || param == "--help" || param == "/?";
      }

      private static bool RequestDeleteExpired(string param)
      {
         return param == "-d" || param == "--delete-expired";
      }
   }
}
