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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Core.Services;
using EasyMonads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : CrypterControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public UserController(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpGet("profile")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserProfileDTO))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetUserProfileAsync([FromQuery] string username,
        CancellationToken cancellationToken)
    {
        IActionResult MakeErrorResponse(GetUserProfileError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                GetUserProfileError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error)
            };
#pragma warning restore CS8524
        }

        Maybe<Guid> userId = _tokenService.TryParseUserId(User);
        return await _userService.GetUserProfileAsync(userId, username, cancellationToken)
            .MatchAsync(
                () => MakeErrorResponse(GetUserProfileError.NotFound),
                Ok);
    }

    [HttpGet("search")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserSearchResult>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> SearchUsersAsync([FromQuery] string keyword, [FromQuery] int index,
        [FromQuery] int count, CancellationToken cancellationToken)
    {
        Guid userId = _tokenService.ParseUserId(User);
        List<UserSearchResult> results =
            await _userService.SearchForUsersAsync(userId, keyword, index, count, cancellationToken);
        return Ok(results);
    }
}
