/*
 * Copyright (C) 2022 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commercial license options.
 */

using Crypter.Contracts.Features.User.Search;
using Crypter.Core.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Queries
{
   public class UserSearchQuery : IRequest<UserSearchQueryResult>
   {
      public Guid RequestorId { get; private set; }
      public string Keyword { get; private set; }
      public int StartingIndex { get; private set; }
      public int Count { get; private set; }

      public UserSearchQuery(Guid requestorId, string keyword, int startingIndex, int count)
      {
         RequestorId = requestorId;
         Keyword = keyword;
         StartingIndex = startingIndex;
         Count = count;
      }
   }

   public enum UserSearchKeywordField
   {
      Username,
      Alias
   }

   public class UserSearchQueryResult
   {
      public int Total { get; private set; }
      public IEnumerable<UserSearchResultDTO> Users { get; private set; }

      public UserSearchQueryResult(int total, IEnumerable<UserSearchResultDTO> users)
      {
         Total = total;
         Users = users;
      }
   }

   public class UserSearchQueryHandler : IRequestHandler<UserSearchQuery, UserSearchQueryResult>
   {
      private readonly DataContext _context;

      public UserSearchQueryHandler(DataContext context)
      {
         _context = context;
      }

      public async Task<UserSearchQueryResult> Handle(UserSearchQuery request, CancellationToken cancellationToken)
      {
         string lowerKeyword = request.Keyword.ToLower();

         IQueryable<Models.User> baseQuery = _context.Users
            .Where(x => x.Username.ToLower().StartsWith(lowerKeyword)
               || x.Profile.Alias.ToLower().StartsWith(lowerKeyword))
            .Where(LinqExtensions.UserProfileIsComplete());

         IQueryable<Models.User> baseQueryWithPrivacy = baseQuery
            .Where(LinqExtensions.UserPrivacyAllowsVisitor(request.RequestorId));

         int totalMatches = await baseQueryWithPrivacy
            .CountAsync(cancellationToken);

         List<UserSearchResultDTO> users = await baseQueryWithPrivacy
            .OrderBy(x => x.Username)
            .Skip(request.StartingIndex)
            .Take(request.Count)
            .Select(x =>
               new UserSearchResultDTO(
                  x.Id,
                  x.Username,
                  x.Profile.Alias
               ))
            .ToListAsync(cancellationToken);

         return new UserSearchQueryResult(totalMatches, users);
      }
   }
}
