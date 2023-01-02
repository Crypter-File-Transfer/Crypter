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

using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface ITransferStorageService
   {
      bool TransferExists(Guid id, TransferItemType itemType, TransferUserType userType);
      Maybe<FileStream> GetTransfer(Guid id, TransferItemType itemType, TransferUserType userType, bool deleteOnReadCompletion);
      Task<bool> SaveTransferAsync(Guid id, TransferItemType itemType, TransferUserType userType, Stream stream, CancellationToken cancellationToken);
      void DeleteTransfer(Guid id, TransferItemType itemType, TransferUserType userType);
   }

   public static class TransferStorageServiceExtensions
   {
      public static void AddTransferStorageService(this IServiceCollection services, Action<TransferStorageSettings> settings)
      {
         if (settings is null)
         {
            throw new ArgumentNullException(nameof(settings));
         }

         services.Configure(settings);
         services.TryAddSingleton<ITransferStorageService, TransferStorageService>();
      }
   }

   public class TransferStorageService : ITransferStorageService
   {
      private readonly TransferStorageSettings _transferStorageSettings;

      public TransferStorageService(IOptions<TransferStorageSettings> transferStorageSettings)
      {
         _transferStorageSettings = transferStorageSettings.Value;
      }

      public bool TransferExists(Guid id, TransferItemType itemType, TransferUserType userType)
      {
         string directory = GetTransferDirectory(itemType, userType);
         string filepath = Path.Join(directory, id.ToString());
         return File.Exists(filepath);
      }

      public Maybe<FileStream> GetTransfer(Guid id, TransferItemType itemType, TransferUserType userType, bool deleteOnReadCompletion)
      {
         string directory = GetTransferDirectory(itemType, userType);
         string filepath = Path.Join(directory, id.ToString());

         FileOptions fileOption = deleteOnReadCompletion
            ? FileOptions.DeleteOnClose
            : FileOptions.None;

         return File.Exists(filepath)
            ? new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, fileOption)
            : Maybe<FileStream>.None;
      }

      public async Task<bool> SaveTransferAsync(Guid id, TransferItemType itemType, TransferUserType userType, Stream stream, CancellationToken cancellationToken)
      {
         string directory = GetTransferDirectory(itemType, userType);
         string filepath = Path.Join(directory, id.ToString());

         try
         {
            if (!Directory.Exists(directory))
            {
               Directory.CreateDirectory(directory);
            }

            using FileStream ciphertextStream = File.OpenWrite(filepath);
            await stream.CopyToAsync(ciphertextStream, cancellationToken);
            await ciphertextStream.FlushAsync(cancellationToken);
            ciphertextStream.Dispose();
         }
         catch (OperationCanceledException)
         {
            DeleteTransfer(id, TransferItemType.Message, userType);
            throw;
         }
         catch (Exception)
         {
            // todo - log something
            DeleteTransfer(id, TransferItemType.Message, userType);
            return false;
         }

         return true;
      }

      public void DeleteTransfer(Guid id, TransferItemType itemType, TransferUserType userType)
      {
         string directory = GetTransferDirectory(itemType, userType);
         string filepath = Path.Join(directory, id.ToString());
         if (File.Exists(filepath))
         {
            File.Delete(filepath);
         }
      }

      private string GetTransferDirectory(TransferItemType itemType, TransferUserType userType)
      {
         string[] directoryParts = new string[]
         {
            _transferStorageSettings.Location,
            userType.ToString().ToLower(),
            itemType.ToString().ToLower()
         };

         return Path.Join(directoryParts);
      }
   }
}
