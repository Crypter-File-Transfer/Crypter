using Crypter.Core.Models;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IBetaKeyService
   {
      Task InsertAsync(string key);
      Task<BetaKey> ReadAsync(string key);
      Task DeleteAsync(string key);
   }
}
