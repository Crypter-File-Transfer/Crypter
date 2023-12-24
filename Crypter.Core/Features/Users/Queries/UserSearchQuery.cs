/*
 * Copyright (C) 2023 Crypter File Transfer
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Core.LinqExpressions;
using Crypter.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Users.Queries;

public sealed record UserSearchQuery(Guid UserId, string Keyword, int Index, int Count)
    : IRequest<List<UserSearchResult>>;

internal sealed class UserSearchQueryHandler : IRequestHandler<UserSearchQuery, List<UserSearchResult>>
{
    private readonly DataContext _dataContext;

    public UserSearchQueryHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public Task<List<UserSearchResult>> Handle(UserSearchQuery request, CancellationToken cancellationToken)
    {
        string lowerKeyword = request.Keyword.ToLower();

        return _dataContext.Users
            .Where(x => x.Username.StartsWith(lowerKeyword)
                        || x.Profile.Alias.ToLower().StartsWith(lowerKeyword))
            .Where(LinqUserExpressions.UserProfileIsComplete())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(request.UserId))
            .OrderBy(x => x.Username)
            .Skip(request.Index)
            .Take(request.Count)
            .Select(x => new UserSearchResult(x.Username, x.Profile.Alias))
            .ToListAsync(cancellationToken);
    }
}
