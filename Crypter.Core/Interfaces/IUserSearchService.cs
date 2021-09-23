using Crypter.Contracts.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface IUserSearchService
   {
      Task<(int total, IEnumerable<UserSearchResultDTO> users)> SearchByUsernameAsync(Guid searchParty, string query, int startingIndex, int count);
      Task<(int total, IEnumerable<UserSearchResultDTO> users)> SearchByAliasAsync(Guid searchParty, string query, int startingIndex, int count);
   }
}
