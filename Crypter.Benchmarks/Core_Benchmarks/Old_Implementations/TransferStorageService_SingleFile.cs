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

namespace Crypter.Benchmarks.Core_Benchmarks.Old_Implementations
{
   /*
   public class TransferStorageService_SingleFile : ITransferStorageService
   {
      private readonly TransferStorageSettings _transferStorageSettings;
      private const string _ciphertextFilename = "ciphertext";
      private const string _headerFilename = "header";

      public TransferStorageService_SingleFile(IOptions<TransferStorageSettings> transferStorageSettings)
      {
         _transferStorageSettings = transferStorageSettings.Value;
      }

      public async Task<bool> SaveTransferAsync(TransferStorageParameters data, CancellationToken cancellationToken)
      {
         string itemDirectory = GetItemDirectory(data.Id, data.ItemType, data.UserType);
         string ciphertextPath = Path.Join(itemDirectory, _ciphertextFilename);
         string headerPath = Path.Join(itemDirectory, _headerFilename);

         try
         {
            Directory.CreateDirectory(itemDirectory);
            await File.WriteAllBytesAsync(headerPath, data.Header, cancellationToken);

            byte[] sizeBuffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(sizeBuffer, data.Ciphertext.Count);

            FileStream ciphertextStream = File.OpenWrite(ciphertextPath);
            await ciphertextStream.WriteAsync(sizeBuffer, cancellationToken);
            foreach (byte[] chunk in data.Ciphertext)
            {
               BinaryPrimitives.WriteInt32LittleEndian(sizeBuffer, chunk.Length);
               await ciphertextStream.WriteAsync(sizeBuffer, cancellationToken);
               await ciphertextStream.WriteAsync(chunk, cancellationToken);
            }

            await ciphertextStream.FlushAsync(cancellationToken);
            ciphertextStream.Close();
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
         string ciphertextPath = Path.Join(itemDirectory, _ciphertextFilename);
         string headerPath = Path.Join(itemDirectory, _headerFilename);

         byte[] headerBytes = await File.ReadAllBytesAsync(headerPath, cancellationToken);

         FileStream ciphertextStream = File.OpenRead(ciphertextPath);
         byte[] sizeBuffer = new byte[4];
         int totalBytesRead = await ciphertextStream.ReadAsync(sizeBuffer.AsMemory(0, 4), cancellationToken);
         int chunkCount = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer);
         List<byte[]> ciphertextChunks = new List<byte[]>(chunkCount);

         while (totalBytesRead < ciphertextStream.Length)
         {
            totalBytesRead += await ciphertextStream.ReadAsync(sizeBuffer.AsMemory(0, 4), cancellationToken);
            int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer);
            byte[] ciphertextBuffer = new byte[chunkSize];
            totalBytesRead += await ciphertextStream.ReadAsync(ciphertextBuffer.AsMemory(0, chunkSize), cancellationToken);
            ciphertextChunks.Add(ciphertextBuffer);
         }
         ciphertextStream.Close();

         return new TransferStorageParameters(id, itemType, userType, headerBytes, ciphertextChunks);
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
   */
}
