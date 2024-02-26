/*
 * Copyright (C) 2024 Crypter File Transfer
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

using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Core.MediatorMonads;
using Crypter.Core.Services;
using Crypter.DataAccess;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserAuthentication.Commands;

public sealed record UserLogoutCommand(ClaimsPrincipal ClaimsPrincipal)
    : IEitherRequest<LogoutError, Unit>;

internal class UserLogoutCommandHandler
    : IEitherRequestHandler<UserLogoutCommand, LogoutError, Unit>
{
    private readonly DataContext _dataContext;

    public UserLogoutCommandHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<Either<LogoutError, Unit>> Handle(UserLogoutCommand request, CancellationToken cancellationToken)
    {
        return await TokenService.TryParseUserId(request.ClaimsPrincipal)
            .ToEither(LogoutError.InvalidToken)
            .Bind(_ => TokenService.TryParseTokenId(request.ClaimsPrincipal)
                .ToEither(LogoutError.InvalidToken))
            .BindAsync<int>(async refreshTokenId => await _dataContext.UserTokens
                .Where(x => x.Id == refreshTokenId).ExecuteDeleteAsync(CancellationToken.None))
            .BindAsync<LogoutError, int, Unit>(_ => Unit.Default);
    }
}
