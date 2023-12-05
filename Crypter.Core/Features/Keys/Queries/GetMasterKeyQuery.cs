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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Keys;
using Crypter.Core.MediatorMonads;
using Crypter.DataAccess;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Keys.Queries;

public record GetMasterKeyQuery(Guid UserId) : IEitherRequest<GetMasterKeyError, GetMasterKeyResponse>;

internal sealed class
    GetMasterKeyQueryHandler : IEitherRequestHandler<GetMasterKeyQuery, GetMasterKeyError, GetMasterKeyResponse>
{
    private readonly DataContext _dataContext;

    public GetMasterKeyQueryHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public Task<Either<GetMasterKeyError, GetMasterKeyResponse>> Handle(GetMasterKeyQuery request, CancellationToken cancellationToken)
    {
        return Either<GetMasterKeyError, GetMasterKeyResponse>.FromRightAsync(
            _dataContext.UserMasterKeys
                .Where(x => x.Owner == request.UserId)
                .Select(x => new GetMasterKeyResponse(x.EncryptedKey, x.Nonce))
                .FirstOrDefaultAsync(cancellationToken), GetMasterKeyError.NotFound);
    }
}
