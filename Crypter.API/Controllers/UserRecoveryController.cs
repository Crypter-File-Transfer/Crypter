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

using System.Net;
using System.Threading.Tasks;
using Crypter.API.Controllers.Base;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.UserRecovery.RequestRecovery;
using Crypter.Common.Contracts.Features.UserRecovery.SubmitRecovery;
using Crypter.Common.Primitives;
using Crypter.Core.Services;
using EasyMonads;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/user/recovery")]
public class UserRecoveryController : CrypterControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHangfireBackgroundService _hangfireBackgroundService;
    private readonly IUserRecoveryService _userRecoveryService;

    public UserRecoveryController(IBackgroundJobClient backgroundJobClient,
        IHangfireBackgroundService hangfireBackgroundService, IUserRecoveryService userRecoveryService)
    {
        _backgroundJobClient = backgroundJobClient;
        _hangfireBackgroundService = hangfireBackgroundService;
        _userRecoveryService = userRecoveryService;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public IActionResult SendRecoveryEmail([FromQuery] string emailAddress)
    {
        if (EmailAddress.TryFrom(emailAddress, out EmailAddress validEmailAddress))
        {
            _backgroundJobClient.Enqueue(() =>
                _hangfireBackgroundService.SendRecoveryEmailAsync(validEmailAddress.Value));
            return Accepted();
        }

        return MakeErrorResponseBase(HttpStatusCode.BadRequest, SendRecoveryEmailError.InvalidEmailAddress);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> SubmitRecoveryAsync([FromBody] SubmitRecoveryRequest request)
    {
        IActionResult MakeErrorResponse(SubmitRecoveryError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                SubmitRecoveryError.PasswordHashFailure
                    or SubmitRecoveryError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError,
                        error),
                SubmitRecoveryError.InvalidUsername
                    or SubmitRecoveryError.WrongRecoveryKey
                    or SubmitRecoveryError.InvalidMasterKey => MakeErrorResponseBase(HttpStatusCode.BadRequest, error),
                SubmitRecoveryError.RecoveryNotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error)
            };
#pragma warning restore CS8524
        }

        return await _userRecoveryService.PerformRecoveryAsync(request)
            .MatchAsync(
                MakeErrorResponse,
                _ => Accepted(),
                MakeErrorResponse(SubmitRecoveryError.UnknownError));
    }
}
