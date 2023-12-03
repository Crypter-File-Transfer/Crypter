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
using Crypter.Core.Extensions;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Services;

public interface IUserService
{
    Task<Maybe<UserEntity>> GetUserEntityAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Maybe<UserEntity>> GetUserEntityAsync(string username, CancellationToken cancellationToken = default);

    Task<Maybe<UserProfileDTO>> GetUserProfileAsync(Maybe<Guid> userId, string username,
        CancellationToken cancellationToken = default);

    Task<List<UserSearchResult>> SearchForUsersAsync(Guid userId, string keyword, int index, int count,
        CancellationToken cancellationToken = default);
    
    Task DeleteUserEntityAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly DataContext _context;

    public UserService(DataContext context)
    {
        _context = context;
    }

    public async Task<Maybe<UserEntity>> GetUserEntityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Maybe<UserEntity>> GetUserEntityAsync(string username,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    public Task<Maybe<UserProfileDTO>> GetUserProfileAsync(Maybe<Guid> userId, string username,
        CancellationToken cancellationToken = default)
    {
        Guid? visitorId = userId.Match<Guid?>(
            () => null,
            x => x);

        return Maybe<UserProfileDTO>.FromAsync(_context.Users
            .Where(x => x.Username == username)
            .Where(LinqUserExpressions.UserProfileIsComplete())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(visitorId))
            .Select(LinqUserExpressions.ToUserProfileDTOForVisitor(visitorId))
            .FirstOrDefaultAsync(cancellationToken));
    }

    public Task<List<UserSearchResult>> SearchForUsersAsync(Guid userId, string keyword, int index, int count,
        CancellationToken cancellationToken = default)
    {
        string lowerKeyword = keyword.ToLower();

        return _context.Users
            .Where(x => x.Username.StartsWith(lowerKeyword)
                        || x.Profile.Alias.ToLower().StartsWith(lowerKeyword))
            .Where(LinqUserExpressions.UserProfileIsComplete())
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(userId))
            .OrderBy(x => x.Username)
            .Skip(index)
            .Take(count)
            .Select(x => new UserSearchResult(x.Username, x.Profile.Alias))
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteUserEntityAsync(Guid id)
    {
        var user = await GetUserEntityAsync(id);
        await user.IfSomeAsync(async x =>
        {
            _context.Users.Remove(x);
            await _context.SaveChangesAsync();
        });
    }
}
