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

using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Core.Models;
using Crypter.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface ITransferStorageService
   {
      Task<bool> SaveTransferAsync(TransferStorageParameters data, CancellationToken cancellationToken);
      Task<Maybe<TransferStorageParameters>> ReadTransferAsync(Guid id, TransferItemType itemType, TransferUserType userType, CancellationToken cancellationToken);
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
      private const string _ciphertextFilenamePrefix = "ciphertext-";
      private const string _headerFilename = "header";

      public TransferStorageService(IOptions<TransferStorageSettings> transferStorageSettings)
      {
         _transferStorageSettings = transferStorageSettings.Value;
      }

      public async Task<bool> SaveTransferAsync(TransferStorageParameters data, CancellationToken cancellationToken)
      {
         string itemDirectory = GetItemDirectory(data.Id, data.ItemType, data.UserType);
         string headerPath = Path.Join(itemDirectory, _headerFilename);

         try
         {
            Directory.CreateDirectory(itemDirectory);
            List<Task> writeTasks = new List<Task>(data.Ciphertext.Count + 1);
            for (int i = 0; i < data.Ciphertext.Count; i++)
            {
               string partPath = Path.Join(itemDirectory, $"{_ciphertextFilenamePrefix}{i}");
               writeTasks.Add(File.WriteAllBytesAsync(partPath, data.Ciphertext[i], cancellationToken));
            }
            writeTasks.Add(File.WriteAllBytesAsync(headerPath, data.Header, cancellationToken));

            await Task.WhenAll(writeTasks);
         }
         catch (OperationCanceledException)
         {
            DeleteDirectoryIfExists(itemDirectory);
            throw;
         }
         catch (Exception)
         {
            // todo - log something
            DeleteDirectoryIfExists(itemDirectory);
            return false;
         }

         return true;
      }

      public async Task<Maybe<TransferStorageParameters>> ReadTransferAsync(Guid id, TransferItemType itemType, TransferUserType userType, CancellationToken cancellationToken)
      {
         string itemDirectory = GetItemDirectory(id, itemType, userType);
         string headerPath = Path.Join(itemDirectory, _headerFilename);

         DirectoryInfo directoryInfo = new DirectoryInfo(itemDirectory);
         FileInfo[] ciphertextFiles = directoryInfo.GetFiles($"{_ciphertextFilenamePrefix}*");

         Task<byte[]>[] readTasks = new Task<byte[]>[ciphertextFiles.Length + 1];
         for (int i = 0; i < ciphertextFiles.Length; i++)
         {
            int fileIndex = int.Parse(ciphertextFiles[i].Name.AsSpan()[_ciphertextFilenamePrefix.Length..]);
            readTasks[fileIndex] = File.ReadAllBytesAsync(ciphertextFiles[i].FullName, cancellationToken);
         }
         readTasks[^1] = File.ReadAllBytesAsync(headerPath, cancellationToken);
         await Task.WhenAll(readTasks);

         List<byte[]> ciphertextChunks = readTasks
            .Take(ciphertextFiles.Length)
            .Select(x => x.Result)
            .ToList();

         return new TransferStorageParameters(id, itemType, userType, readTasks[^1].Result, ciphertextChunks);
      }

      public void DeleteTransfer(Guid id, TransferItemType itemType, TransferUserType userType)
      {
         var itemDirectory = GetItemDirectory(id, itemType, userType);
         DeleteDirectoryIfExists(itemDirectory);
      }

      private string GetItemDirectory(Guid id, TransferItemType itemType, TransferUserType userType)
      {
         string[] pathParts = new string[]
         {
            _transferStorageSettings.Location,
            userType.ToString().ToLower(),
            itemType.ToString().ToLower(),
            id.ToString()
         };

         return Path.Join(pathParts);
      }

      private static void DeleteDirectoryIfExists(string path)
      {
         if (Directory.Exists(path))
         {
            Directory.Delete(path, true);
         }
      }
   }
}
