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

using Crypter.API.Controllers.Base;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Threading;
using Crypter.Core.Features.Version.Queries;
using Crypter.Common.Contracts.Features.Version;

namespace Crypter.API.Controllers
{
    [ApiController]
    [Route("api/version")]
    public class VersionController: CrypterControllerBase
    {
        private readonly ISender _sender;

        public VersionController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VersionResponse))]
        public async Task<IActionResult> GetVersionAsync(CancellationToken cancellationToken)
        {
            GetVersionsQuery request = new GetVersionsQuery();
            GetVersionsResult result = await _sender.Send(request, cancellationToken);
            VersionResponse response = new VersionResponse(result.Version, result.VersionHash, result.VersionUrl, result.IsRelease);
            return Ok(response);
        }
    }
}
