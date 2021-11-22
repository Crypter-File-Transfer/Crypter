using System;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface ITransferItemStorageService
   {
      Task<bool> SaveAsync(Guid id, byte[] data);
      Task<byte[]> ReadAsync(Guid id);
      bool Delete(Guid id);
   }
}
