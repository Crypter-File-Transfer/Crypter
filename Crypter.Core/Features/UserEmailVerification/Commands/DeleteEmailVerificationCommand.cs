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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = EasyMonads.Unit;

namespace Crypter.Core.Features.UserEmailVerification.Commands;

public sealed record DeleteEmailVerificationCommand(Guid UserId, Guid Code) : IRequest<Unit>;

internal sealed class DeleteEmailVerificationCommandHandler : IRequestHandler<DeleteEmailVerificationCommand, Unit>
{
    private readonly DataContext _dataContext;

    public DeleteEmailVerificationCommandHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<Unit> Handle(DeleteEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        await _dataContext.UserEmailChangeRequests
            .Where(x => x.Owner == request.UserId && x.Code == request.Code)
            .ExecuteDeleteAsync(CancellationToken.None);

        return Unit.Default;
    }
}
