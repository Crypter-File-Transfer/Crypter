using System;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IUserPublicKeyPairService<T>
   {
      Task<IUserPublicKeyPair> GetUserPublicKeyPairAsync(Guid userId);
      Task<string> GetUserPublicKeyAsync(Guid userId);
      Task<bool> InsertUserPublicKeyPairAsync(Guid userId, string privateKey, string publicKey);
   }
}
