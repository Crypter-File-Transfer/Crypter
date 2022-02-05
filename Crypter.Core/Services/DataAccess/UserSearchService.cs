﻿/*
 * Copyright (C) 2021 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Contracts.Common.Enum;
using Crypter.Contracts.Features.User.Search;
using Crypter.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserSearchService : IUserSearchService
   {
      private readonly DataContext Context;

      public UserSearchService(DataContext context)
      {
         Context = context;
      }

      public async Task<(int total, IEnumerable<UserSearchResultDTO> users)> SearchByUsernameAsync(Guid searchParty, string query, int startingIndex, int count, CancellationToken cancellationToken)
      {
         var lowerUsername = query.ToLower();

         int totalMatches = await Context.Users
            .Where(x => x.Username.ToLower().StartsWith(lowerUsername))
            .Where(x => x.PrivacySetting.Visibility == UserVisibilityLevel.Everyone
               || (x.PrivacySetting.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .CountAsync(cancellationToken);

         var users = await Context.Users
            .Where(x => x.Username.ToLower().StartsWith(lowerUsername))
            .Where(x => x.PrivacySetting.Visibility == UserVisibilityLevel.Everyone
               || (x.PrivacySetting.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .OrderBy(x => x.Username)
            .Skip(startingIndex)
            .Take(count)
            .Select(x => 
               new UserSearchResultDTO(
                  x.Id,
                  x.Username,
                  x.Profile.Alias
               ))
            .ToListAsync(cancellationToken);

         return (totalMatches, users);
      }

      public async Task<(int total, IEnumerable<UserSearchResultDTO> users)> SearchByAliasAsync(Guid searchParty, string query, int startingIndex, int count, CancellationToken cancellationToken)
      {
         var lowerAlias = query.ToLower();

         int totalMatches = await Context.UserProfiles
            .Where(x => x.Alias.ToLower().StartsWith(lowerAlias))
            .Where(x => x.User.PrivacySetting.Visibility == UserVisibilityLevel.Everyone
               || (x.User.PrivacySetting.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .CountAsync(cancellationToken);

         var users = await Context.UserProfiles
            .Where(x => x.Alias.ToLower().StartsWith(lowerAlias))
            .Where(x => x.User.PrivacySetting.Visibility == UserVisibilityLevel.Everyone
               || (x.User.PrivacySetting.Visibility == UserVisibilityLevel.Authenticated && searchParty != Guid.Empty))
            .OrderBy(x => x.Alias)
            .Skip(startingIndex)
            .Take(count)
            .Select(x =>
               new UserSearchResultDTO(
                  x.Owner,
                  x.User.Username,
                  x.Alias
               ))
            .ToListAsync(cancellationToken);

         return (totalMatches, users);
      }
   }
}
