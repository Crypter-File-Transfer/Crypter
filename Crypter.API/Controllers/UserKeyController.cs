/*
 *This file is part of the Crypter file transfer project.
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
 *You can be released from the requirements of the aforementioned license
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
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Core.Services;
using EasyMonads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Crypter.API.Controllers;

[ApiController]
[Route("api/user/key")]
public class UserKeyController : CrypterControllerBase
{
    private readonly IUserKeysService _userKeysService;

    public UserKeyController(IUserKeysService userKeysService)
    {
        _userKeysService = userKeysService;
    }

    [HttpGet("master")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetMasterKeyResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetMasterKeyAsync(CancellationToken cancellationToken)
    {
        IActionResult MakeErrorResponse(GetMasterKeyError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                GetMasterKeyError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                GetMasterKeyError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error)
            };
#pragma warning restore CS8524
        }
        
        return await _userKeysService.GetMasterKeyAsync(UserId, cancellationToken)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(GetMasterKeyError.UnknownError));
    }

    [HttpPost("master")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> InsertMasterKeyAsync(InsertMasterKeyRequest request)
    {
        IActionResult MakeErrorResponse(InsertMasterKeyError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                InsertMasterKeyError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                InsertMasterKeyError.Conflict => MakeErrorResponseBase(HttpStatusCode.Conflict, error),
                InsertMasterKeyError.InvalidPassword
                    or InsertMasterKeyError.InvalidMasterKey => MakeErrorResponseBase(HttpStatusCode.BadRequest, error)
            };
#pragma warning restore CS8524
        }
        
        return await _userKeysService.UpsertMasterKeyAsync(UserId, request, false)
            .MatchAsync(
                MakeErrorResponse,
                _ => Ok(),
                MakeErrorResponse(InsertMasterKeyError.UnknownError));
    }

    [HttpPost("master/recovery-proof/challenge")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetMasterKeyRecoveryProofResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetMasterKeyRecoveryProofAsync(GetMasterKeyRecoveryProofRequest request,
        CancellationToken cancellationToken)
    {
        IActionResult MakeErrorResponse(GetMasterKeyRecoveryProofError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                GetMasterKeyRecoveryProofError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError,
                    error),
                GetMasterKeyRecoveryProofError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error),
                GetMasterKeyRecoveryProofError.InvalidCredentials => MakeErrorResponseBase(HttpStatusCode.BadRequest,
                    error)
            };
#pragma warning restore CS8524
        }
        
        return await _userKeysService.GetMasterKeyProofAsync(UserId, request, cancellationToken)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(GetMasterKeyRecoveryProofError.UnknownError));
    }

    [HttpGet("private")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetPrivateKeyResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> GetPrivateKeyAsync(CancellationToken cancellationToken)
    {
        IActionResult MakeErrorResponse(GetPrivateKeyError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                GetPrivateKeyError.UnkownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                GetPrivateKeyError.NotFound => MakeErrorResponseBase(HttpStatusCode.NotFound, error)
            };
#pragma warning restore CS8524
        }
        
        return await _userKeysService.GetPrivateKeyAsync(UserId, cancellationToken)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(GetPrivateKeyError.UnkownError));
    }

    [HttpPut("private")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InsertKeyPairResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrorResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorResponse))]
    public async Task<IActionResult> InsertKeyPairAsync([FromBody] InsertKeyPairRequest body)
    {
        IActionResult MakeErrorResponse(InsertKeyPairError error)
        {
#pragma warning disable CS8524
            return error switch
            {
                InsertKeyPairError.UnknownError => MakeErrorResponseBase(HttpStatusCode.InternalServerError, error),
                InsertKeyPairError.KeyPairAlreadyExists => MakeErrorResponseBase(HttpStatusCode.Conflict, error),
            };
#pragma warning restore CS8524
        }
        
        return await _userKeysService.InsertKeyPairAsync(UserId, body)
            .MatchAsync(
                MakeErrorResponse,
                Ok,
                MakeErrorResponse(InsertKeyPairError.UnknownError));
    }
}
