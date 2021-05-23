using Crypter.Contracts.Enum;
using Crypter.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.DataAccess.Interfaces
{
    public interface IUserService
    {
        Task<InsertUserResult> InsertAsync(string username, string password, string email = null);
        Task<User> ReadAsync(Guid id);
        Task<UpdateUserCredentialsResult> UpdateCredentialsAsync(Guid id, string username, string newPassword, string email = null);
        Task<UpdateUserPreferencesResult> UpdatePreferencesAsync(Guid id, string publicAlias, bool isPublic, bool allowAnonymousFiles, bool allowAnonymousMessages);
        Task DeleteAsync(Guid id);

        Task<User> AuthenticateAsync(string username, string password);

        Task<(int total, IEnumerable<User> users)> SearchByUsernameAsync(string username, int startingIndex, int count);
        Task<(int total, IEnumerable<User> users)> SearchByPublicAliasAsync(string publicAlias, int startingIndex, int count);

        Task<bool> IsUsernameAvailableAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email);
    }
}
