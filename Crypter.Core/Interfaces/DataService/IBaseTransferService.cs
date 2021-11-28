using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IBaseTransferService<T>
   {
      Task InsertAsync(T item);
      Task<T> ReadAsync(Guid id);
      Task DeleteAsync(Guid id);

      Task<IEnumerable<T>> FindBySenderAsync(Guid senderId);
      Task<IEnumerable<T>> FindByRecipientAsync(Guid recipientId);
      Task<IEnumerable<T>> FindExpiredAsync();
      Task<long> GetAggregateSizeAsync();
   }
}
