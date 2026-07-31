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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Crypter.API.Methods;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Core.Features.UserContacts.Commands;
using Crypter.Core.Services;
using EasyMonads;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Endpoints.UserContacts;

[Handler]
[MapPost("api/user/contact")]
[Authorize]
public static partial class AddUserContactEndpoint
{
    public sealed record Request
    {
        [FromQuery]
        public required string Username { get; init; }
    }

    internal static void CustomizeEndpoint(RouteHandlerBuilder endpoint) =>
        endpoint
            .Produces<UserContact>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);

    private static async ValueTask<IResult> HandleAsync(
        [AsParameters] Request request,
        IHttpContextAccessor httpContextAccessor,
        AddUserContactCommand.Handler handler,
        CancellationToken cancellationToken)
    {
        Guid userId = TokenService.ParseUserId(httpContextAccessor.HttpContext!.User);
        AddUserContactCommand.Command command = new AddUserContactCommand.Command(userId, request.Username);

        Either<AddUserContactError, UserContact> result = await handler.HandleAsync(command, cancellationToken);
        return result.Match(
            MakeErrorResponse,
            x => Results.Ok(x),
            MakeErrorResponse(AddUserContactError.UnknownError));
    }

    private static IResult MakeErrorResponse(AddUserContactError error)
    {
#pragma warning disable CS8524
        return error switch
        {
            AddUserContactError.UnknownError => EndpointResults.MakeErrorResponse(HttpStatusCode.InternalServerError, error),
            AddUserContactError.NotFound => EndpointResults.MakeErrorResponse(HttpStatusCode.NotFound, error),
            AddUserContactError.InvalidUser => EndpointResults.MakeErrorResponse(HttpStatusCode.BadRequest, error)
        };
#pragma warning restore CS8524
    }
}
