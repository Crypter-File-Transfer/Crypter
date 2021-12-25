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

using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public class TransferItemStorageService : ITransferItemStorageService
   {
      private readonly string StoragePath;
      private readonly TransferItemType ItemType;

      public TransferItemStorageService(string baseStoragePath, TransferItemType itemType)
      {
         StoragePath = Path.Join(baseStoragePath, $"{itemType}s"); // Make it plural
         ItemType = itemType;
      }

      public async Task<bool> SaveAsync(Guid id, byte[] data, CancellationToken cancellationToken)
      {
         var newDirectory = Path.Join(StoragePath, id.ToString().ToLower());
         Directory.CreateDirectory(newDirectory);
         var newFile = Path.Join(newDirectory, ItemType.ToString());
         try
         {
            await File.WriteAllBytesAsync(newFile, data, cancellationToken);
         }
         catch (Exception)
         {
            return false;
         }
         return true;
      }

      public async Task<byte[]> ReadAsync(Guid id, CancellationToken cancellationToken)
      {
         var itemDirectory = Path.Join(StoragePath, id.ToString().ToLower());
         var file = Path.Join(itemDirectory, ItemType.ToString());
         return await File.ReadAllBytesAsync(file, cancellationToken);
      }

      public bool Delete(Guid id)
      {
         var itemDirectory = Path.Join(StoragePath, id.ToString().ToLower());
         try
         {
            Directory.Delete(itemDirectory, true);
         }
         catch (Exception)
         {
            return false;
         }
         return true;
      }
   }
}
