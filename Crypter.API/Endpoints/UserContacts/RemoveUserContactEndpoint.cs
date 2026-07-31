/*
 * Copyright (C) 2026 Crypter File Transfer
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
using System.Threading;
using System.Threading.Tasks;
using Crypter.Core.Features.UserContacts.Commands;
using Crypter.Core.Services;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Endpoints.UserContacts;

[Handler]
[MapDelete("api/user/contact")]
[Authorize]
public static partial class RemoveUserContactEndpoint
{
    public sealed record Request
    {
        [FromQuery]
        public required string Username { get; init; }
    }

    internal static void CustomizeEndpoint(RouteHandlerBuilder endpoint) =>
        endpoint
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

    private static async ValueTask<IResult> HandleAsync(
        [AsParameters] Request request,
        IHttpContextAccessor httpContextAccessor,
        RemoveUserContactCommand.Handler handler,
        CancellationToken cancellationToken)
    {
        Guid userId = TokenService.ParseUserId(httpContextAccessor.HttpContext!.User);
        RemoveUserContactCommand.Command command = new RemoveUserContactCommand.Command(userId, request.Username);

        await handler.HandleAsync(command, cancellationToken);
        return Results.Ok();
    }
}
