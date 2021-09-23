using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using System;
using System.IO;
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

      public async Task<bool> SaveAsync(Guid id, byte[] data)
      {
         var newDirectory = Path.Join(StoragePath, id.ToString().ToLower());
         Directory.CreateDirectory(newDirectory);
         var newFile = Path.Join(newDirectory, ItemType.ToString());
         try
         {
            await File.WriteAllBytesAsync(newFile, data);
         }
         catch (Exception)
         {
            return false;
         }
         return true;
      }

      public async Task<byte[]> ReadAsync(Guid id)
      {
         var itemDirectory = Path.Join(StoragePath, id.ToString().ToLower());
         var file = Path.Join(itemDirectory, ItemType.ToString());
         return await File.ReadAllBytesAsync(file);
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
