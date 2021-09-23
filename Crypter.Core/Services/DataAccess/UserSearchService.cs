using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserSearchService : IUserSearchService
   {
      private readonly DataContext _context;

      public UserSearchService(DataContext context)
      {
         _context = context;
      }

      public async Task<(int total, IEnumerable<UserSearchResultDTO> users)> SearchByUsernameAsync(Guid searchParty, string query, int startingIndex, int count)
      {
         var lowerUsername = query.ToLower();

         int totalMatches = await _context.User
            .Where(x => x.Username.ToLower().StartsWith(lowerUsername))
            .Where(x => x.Privacy.Visibility == UserVisibilityLevel.Everyone
               || (x.Privacy.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .CountAsync();

         var users = await _context.User
            .Where(x => x.Username.ToLower().StartsWith(lowerUsername))
            .Where(x => x.Privacy.Visibility == UserVisibilityLevel.Everyone
               || (x.Privacy.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .OrderBy(x => x.Username)
            .Skip(startingIndex)
            .Take(count)
            .Select(x => 
               new UserSearchResultDTO(
                  x.Id,
                  x.Username,
                  x.Profile.Alias
               ))
            .ToListAsync();

         return (totalMatches, users);
      }

      public async Task<(int total, IEnumerable<UserSearchResultDTO> users)> SearchByAliasAsync(Guid searchParty, string query, int startingIndex, int count)
      {
         var lowerAlias = query.ToLower();

         int totalMatches = await _context.UserProfile
            .Where(x => x.Alias.ToLower().StartsWith(lowerAlias))
            .Where(x => x.User.Privacy.Visibility == UserVisibilityLevel.Everyone
               || (x.User.Privacy.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .CountAsync();

         var users = await _context.UserProfile
            .Where(x => x.Alias.ToLower().StartsWith(lowerAlias))
            .Where(x => x.User.Privacy.Visibility == UserVisibilityLevel.Everyone
               || (x.User.Privacy.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .OrderBy(x => x.Alias)
            .Skip(startingIndex)
            .Take(count)
            .Select(x =>
               new UserSearchResultDTO(
                  x.Owner,
                  x.User.Username,
                  x.Alias
               ))
            .ToListAsync();

         return (totalMatches, users);
      }
   }
}
