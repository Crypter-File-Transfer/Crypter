using System;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IUserProfileService
   {
      Task<IUserProfile> ReadAsync(Guid id);
      Task<bool> UpdateAsync(Guid id, string alias, string about);
   }
}
