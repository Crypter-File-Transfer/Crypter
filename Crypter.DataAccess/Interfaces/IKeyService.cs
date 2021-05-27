using Crypter.DataAccess.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.DataAccess.Interfaces
{
    public interface IKeyService
    {
        Task<Key> GetUserPersonalKeyAsync(Guid userId);
        Task<bool> InsertUserPersonalKeyAsync(Guid userId, string privateKey, string publicKey);
        Task<string> GetUserPublicKey(Guid userId); 
    }
}
