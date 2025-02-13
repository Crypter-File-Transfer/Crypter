/*
 * Copyright (C) 2025 Crypter File Transfer
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

using Crypter.API.Controllers.Base;
using Crypter.Common.Contracts.Features.Keys.GetJwks;
using Crypter.Core.Features.Keys.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
    [ApiController]
    [Route(".well-known")]
    public class JwtController : CrypterControllerBase
    {
        private const string CONFIG_PATH = "openid-configuration";
        private const string JWKS_PATH = "jwks";

        private readonly ISender _sender;

        public JwtController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet(CONFIG_PATH)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OpenIdConfigResponse))]
        [Produces("application/json")]
        public IActionResult GetJwtConfig()
        {
            string fullUrl = HttpContext.Request.GetDisplayUrl();
            return Ok(new OpenIdConfigResponse(fullUrl.Replace(CONFIG_PATH, JWKS_PATH)));
        }

        [HttpGet(JWKS_PATH)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetJwksResponse))]
        [Produces("application/json")]
        public async Task<IActionResult> GetJwks(CancellationToken cancellationToken)
        {
            List<JsonWebKeyModel> result = await _sender.Send(new GetJwksQuery(), cancellationToken);
            GetJwksResponse response = new GetJwksResponse(result);
            return Ok(response);
        }
    }
}
