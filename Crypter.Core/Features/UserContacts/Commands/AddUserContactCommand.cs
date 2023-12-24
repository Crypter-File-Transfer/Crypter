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
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.Contacts.RequestErrorCodes;
using Crypter.Core.LinqExpressions;
using Crypter.Core.MediatorMonads;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserContacts.Commands;

public record AddUserContactCommand(Guid UserId, string ContactUsername)
    : IEitherRequest<AddUserContactError, UserContact>;

internal class AddUserContactCommandHandler
    : IEitherRequestHandler<AddUserContactCommand, AddUserContactError, UserContact>
{
    private readonly DataContext _dataContext;

    public AddUserContactCommandHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<Either<AddUserContactError, UserContact>> Handle(AddUserContactCommand request, CancellationToken cancellationToken)
    {
        string lowerContactUsername = request.ContactUsername.ToLower();

        var foundUser = await _dataContext.Users
            .Where(x => x.Username.ToLower() == lowerContactUsername)
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(request.UserId))
            .Select(x => new { x.Id, x.Username, x.Profile.Alias })
            .FirstOrDefaultAsync(CancellationToken.None);

        if (foundUser is null)
        {
            return AddUserContactError.NotFound;
        }

        if (request.UserId == foundUser.Id)
        {
            return AddUserContactError.InvalidUser;
        }

        bool contactExists = await _dataContext.UserContacts
            .Where(x => x.OwnerId == request.UserId)
            .Where(x => x.ContactId == foundUser.Id)
            .AnyAsync(CancellationToken.None);

        if (!contactExists)
        {
            UserContactEntity newContactEntity = new UserContactEntity(request.UserId, foundUser.Id);
            _dataContext.UserContacts.Add(newContactEntity);
            await _dataContext.SaveChangesAsync(CancellationToken.None);
        }

        return new UserContact(foundUser.Username, foundUser.Alias);
    }
}
