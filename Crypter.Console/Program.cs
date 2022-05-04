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

using Crypter.Console.Jobs;
using Crypter.Core;
using Crypter.Core.Entities.Interfaces;
using Crypter.Core.Interfaces;
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
            .AddSingleton<IBaseTransferService<IMessageTransfer>, MessageTransferItemService>()
            .AddSingleton<IBaseTransferService<IFileTransfer>, FileTransferItemService>()
            .BuildServiceProvider();

         if (args == null || args.Length == 0 || HelpRequired(args[0]))
         {
            Help.DisplayHelp();
            return 0;
         }

         if (RequestDeleteExpired(args[0]))
         {
            var deleteJob = new DeleteExpired(configuration["EncryptedFileStore"],
               serviceProvider.GetService<IBaseTransferService<IMessageTransfer>>()!,
               serviceProvider.GetService<IBaseTransferService<IFileTransfer>>()!,
               serviceProvider.GetService<ILogger<DeleteExpired>>()!);

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
