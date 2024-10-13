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

using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Crypter.API.Controllers.Base;
using Crypter.API.Methods;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserAuthentication.PasswordChange;
using Crypter.Core.Features.UserAuthentication.Commands;
using Crypter.Core.Features.UserAuthentication.Queries;
using EasyMonads;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/user/authentication")]
public class UserAuthenticationController : CrypterControllerBase
{
    private readonly ISender _sender;

    public UserAuthenticationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Handle a registration request.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("register")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> RegisterAsync([FromBody] RegistrationRequest request)
    {
        IActionResult MakeErrorResponse(RegistrationError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                RegistrationError.UnknownError
                    or RegistrationError.PasswordHashFailure
                    or RegistrationError.InvalidPasswordConfirm => MakeErrorResponseBase(
                        HttpStatusCode.InternalServerError, error),
                RegistrationError.InvalidUsername
                    or RegistrationError.InvalidPassword
                    or RegistrationError.InvalidEmailAddress
                    or RegistrationError.OldPasswordVersion => MakeErrorResponseBase(HttpStatusCode.BadRequest, error),
                RegistrationError.UsernameTaken
                    or RegistrationError.EmailAddressTaken => MakeErrorResponseBase(HttpStatusCode.Conflict, error)
            };
#pragma warning restore CS8524
        }

        string requestUserAgent = HeadersParser.GetUserAgent(HttpContext.Request.Headers);
        UserRegistrationCommand command = new UserRegistrationCommand(request, requestUserAgent);
        return await _sender.Send(command)
            .MatchAsync(
                MakeErrorResponse,
                _ => Ok(),
                MakeErrorResponse(RegistrationError.UnknownError));
    }

    /// <summary>
    /// Handle a login request.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        IActionResult MakeErrorResponse(LoginError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                LoginError.UnknownError
                    or LoginError.PasswordHashFailure => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                LoginError.InvalidUsername
                    or LoginError.InvalidPassword
                    or LoginError.InvalidTokenTypeRequested
                    or LoginError.ExcessiveFailedLoginAttempts
                    or LoginError.InvalidPasswordVersion => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
        }

        string requestUserAgent = HeadersParser.GetUserAgent(HttpContext.Request.Headers);
        UserLoginCommand command = new UserLoginCommand(request, requestUserAgent);
        return await _sender.Send(command)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(LoginError.UnknownError));
    }

    /// <summary>
    /// Trade in a valid refresh token for a new authentication token and refresh token.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// This action will accept a valid, un-expired refresh token. In exchange, it will respond with a fresh authentication token
    /// and a fresh refresh token of the same type (i.e. a short-term "session" token vs a long-term "device" token).
    /// If the client wants to switch the type of refresh token, it should perform a new login.
    /// The refresh token should be provided in the Authorization header.
    /// </remarks>
    [HttpGet("refresh")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RefreshResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> RefreshAsync()
    {
        IActionResult MakeErrorResponse(RefreshError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                RefreshError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                RefreshError.UserNotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error),
                RefreshError.InvalidToken => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
        }


        string requestUserAgent = HeadersParser.GetUserAgent(HttpContext.Request.Headers);
        RefreshUserSessionCommand request = new RefreshUserSessionCommand(User, requestUserAgent);
        return await _sender.Send(request)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(RefreshError.UnknownError));
    }

    /// <summary>
    /// Handle a password challenge request for an authorized user.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("password/challenge")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> PasswordChallengeAsync([FromBody] PasswordChallengeRequest request,
        CancellationToken cancellationToken)
    {
        IActionResult MakeErrorResponse(PasswordChallengeError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                PasswordChallengeError.UnknownError
                    or PasswordChallengeError.PasswordHashFailure => MakeErrorResponseBase(
                        HttpStatusCode.InternalServerError, error),
                PasswordChallengeError.InvalidPassword
                    or PasswordChallengeError.PasswordNeedsMigration => MakeErrorResponseBase(HttpStatusCode.BadRequest,
                        error)
            };
#pragma warning restore CS8524
        }

        TestUserPasswordQuery query = new TestUserPasswordQuery(UserId, request);
        return await _sender.Send(query, cancellationToken)
            .MatchAsync(
                MakeErrorResponse,
                _ => Ok(),
                MakeErrorResponse(PasswordChallengeError.UnknownError));
    }

    /// <summary>
    /// Handle a request to change the password for an authorized user
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    public async Task<IActionResult> PasswordChangeAsync([FromBody] PasswordChangeRequest request)
    {
        IActionResult MakeErrorResponse(PasswordChangeError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                PasswordChangeError.UnknownError
                    or PasswordChangeError.PasswordHashFailure => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                PasswordChangeError.InvalidPassword
                    or PasswordChangeError.InvalidOldPasswordVersion
                    or PasswordChangeError.InvalidNewPasswordVersion => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
        }
        
        ChangeUserPasswordCommand command = new ChangeUserPasswordCommand(UserId, request);
        return await _sender.Send(command)
            .MatchAsync(
                MakeErrorResponse,
                _ => Ok(),
                MakeErrorResponse(PasswordChangeError.UnknownError));
    }
    
    /// <summary>
    /// Clears the provided refresh token from the database, ensuring it cannot be used for subsequent requests.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// The refresh token should be provided in the Authorization header.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> Logout()
    {
        IActionResult MakeErrorResponse(LogoutError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                LogoutError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                LogoutError.InvalidToken => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
        }

        UserLogoutCommand request = new UserLogoutCommand(User);
        return await _sender.Send(request)
            .MatchAsync(
                MakeErrorResponse,
                _ => Ok(),
                MakeErrorResponse(LogoutError.UnknownError));
    }
}
