using Crypter.DataAccess.Models;
using System.Threading.Tasks;

namespace Crypter.DataAccess.Interfaces
{
    public interface IBetaKeyService
    {
        Task InsertAsync(string key);
        Task<BetaKey> ReadAsync(string key);
        Task DeleteAsync(string key);
    }
}
