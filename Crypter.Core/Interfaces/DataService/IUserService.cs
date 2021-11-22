using Crypter.Contracts.Enum;
using Crypter.Core.Models;
using System;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IUserService
   {
      Task<Guid> InsertAsync(string username, string password, string email = null);
      Task<IUser> ReadAsync(Guid id);
      Task<IUser> ReadAsync(string username);
      Task<UpdateContactInfoResult> UpdateContactInfoAsync(Guid id, string email, string currentPassword);
      Task UpdateEmailAddressVerification(Guid id, bool isVerified);
      Task DeleteAsync(Guid id);

      Task<User> AuthenticateAsync(string username, string password);
      Task UpdateLastLoginTime(Guid id, DateTime dateTime);

      Task<bool> IsUsernameAvailableAsync(string username);
      Task<bool> IsEmailAddressAvailableAsync(string email);
   }
}
