using Crypter.Console.Jobs;
using Crypter.DataAccess;
using Crypter.DataAccess.EntityFramework;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
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
            .AddSingleton<IBaseItemService<MessageItem>, MessageItemService>()
            .AddSingleton<IBaseItemService<FileItem>, FileItemService>()
            .BuildServiceProvider();

         if (args == null || args.Length == 0 || HelpRequired(args[0]))
         {
            Help.DisplayHelp();
            return 0;
         }

         if (RequestDeleteExpired(args[0]))
         {
            var deleteJob = new DeleteExpired(configuration["EncryptedFileStore"],
               serviceProvider.GetService<IBaseItemService<MessageItem>>(),
               serviceProvider.GetService<IBaseItemService<FileItem>>(),
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
