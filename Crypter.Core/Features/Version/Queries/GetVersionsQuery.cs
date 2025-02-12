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

using Crypter.Common.Services;
using EasyMonads;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.Version.Queries
{
    public record GetVersionsQuery : IRequest<GetVersionsResult>;

    public record GetVersionsResult(string Version, string? VersionHash, string VersionUrl, bool IsRelease);

    internal sealed class GetVersionQueryHandler : IRequestHandler<GetVersionsQuery, GetVersionsResult>
    {
        private readonly IVersionService _versionService;

        public GetVersionQueryHandler(IVersionService versionService)
        {
            _versionService = versionService;
        }

        public Task<GetVersionsResult> Handle(GetVersionsQuery request, CancellationToken cancellationToken)
        {
            return new GetVersionsResult(
                _versionService.ProductVersion,
                _versionService.VersionHash,
                _versionService.VersionUrl,
                _versionService.IsRelease).AsTask();
        }
    }


}
