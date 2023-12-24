﻿/*
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Core.LinqExpressions;
using Crypter.Core.MediatorMonads;
using Crypter.DataAccess;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Users.Queries;

public sealed record UserProfileQuery(Maybe<Guid> UserId, string Username) : IMaybeRequest<UserProfile>;

internal sealed class UserProfileQueryHandler : IMaybeRequestHandler<UserProfileQuery, UserProfile>
{
    private readonly DataContext _dataContext;

    public UserProfileQueryHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public Task<Maybe<UserProfile>> Handle(UserProfileQuery request, CancellationToken cancellationToken)
    {
        Guid? visitorId = request.UserId.Match<Guid?>(
            () => null,
            x => x);

        return Maybe<UserProfile>.FromAsync(_dataContext.Users
            .Where(x => x.Username == request.Username)
            .Where(LinqUserExpressions.UserProfileIsComplete())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(visitorId))
            .Select(LinqUserExpressions.ToUserProfileForVisitor(visitorId))
            .FirstOrDefaultAsync(cancellationToken));
    }
}