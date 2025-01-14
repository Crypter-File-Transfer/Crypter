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
using System.Threading;
using System.Threading.Tasks;
using Crypter.API.Controllers.Base;
using Crypter.Common.Contracts.Features.UserConsents;
using Crypter.Core.Features.UserConsent.Commands;
using Crypter.Core.Features.UserConsent.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/user/consent")]
public class ConsentController : CrypterControllerBase
{
    private readonly ISender _sender;

    public ConsentController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<UserConsentType, DateTimeOffset?>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> GetUserConsentsAsync(CancellationToken cancellationToken)
    {
        GetUserConsentsQuery query = new GetUserConsentsQuery(UserId);
        Dictionary<UserConsentType, DateTimeOffset?> result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }
    
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> ConsentAsync(UserConsentRequest request)
    {
        SaveUserConsentCommand command = new SaveUserConsentCommand(UserId, request.ConsentType);
        await _sender.Send(command, CancellationToken.None);
        return Ok();
    }
}
