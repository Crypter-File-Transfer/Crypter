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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Core.Services;
using Crypter.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Transfer.Queries;

public sealed record UserReceivedFilesQuery(Guid UserId)
    : IRequest<IEnumerable<UserReceivedFileDTO>>;

internal class UserReceivedFilesQueryHandler
    : IRequestHandler<UserReceivedFilesQuery, IEnumerable<UserReceivedFileDTO>>
{
    private readonly DataContext _dataContext;
    private readonly IHashIdService _hashIdService;
    
    public UserReceivedFilesQueryHandler(DataContext dataContext, IHashIdService hashIdService)
    {
        _dataContext = dataContext;
        _hashIdService = hashIdService;
    }
    
    public async Task<IEnumerable<UserReceivedFileDTO>> Handle(UserReceivedFilesQuery request, CancellationToken cancellationToken)
    {
        var receivedFiles = await _dataContext.UserFileTransfers
            .Where(x => x.RecipientId == request.UserId)
            .Where(x => !x.Parts)
            .OrderBy(x => x.Expiration)
            .Select(x => new { x.Id, x.FileName, x.Sender!.Username, x.Sender!.Profile!.Alias, x.Expiration })
            .ToListAsync(cancellationToken);

        return receivedFiles
            .Select(x =>
                new UserReceivedFileDTO(_hashIdService.Encode(x.Id), x.FileName, x.Username, x.Alias, x.Expiration));
    }
}
