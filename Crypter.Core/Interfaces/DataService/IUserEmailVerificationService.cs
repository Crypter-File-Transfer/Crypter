using System;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IUserEmailVerificationService
   {
      Task<bool> InsertAsync(Guid userId, Guid code, byte[] verificationKey);
      Task<IUserEmailVerification> ReadAsync(Guid userId);
      Task<IUserEmailVerification> ReadCodeAsync(Guid code);
      Task DeleteAsync(Guid userId);
   }
}
