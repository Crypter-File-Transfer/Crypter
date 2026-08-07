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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Enums;
using Crypter.DataAccess;
using Crypter.DataAccess.Entities;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserContacts.Queries;

[Handler]
public static partial class GetUserContactsQuery
{
    public sealed record Query(Guid UserId);

    private static async ValueTask<List<UserContact>> HandleAsync(
        Query request,
        DataContext dataContext,
        CancellationToken cancellationToken)
    {
        return await dataContext.UserContacts
            .Where(x => x.OwnerId == request.UserId)
            .Select(x => x.Contact)
            .Select(ToUserContactDto(request.UserId))
            .ToListAsync(cancellationToken);
    }

    private static Expression<Func<UserEntity?, UserContact>> ToUserContactDto(Guid? visitorId)
    {
        return x => x != null
                && (x.Id == visitorId
                    || x.PrivacySetting!.Visibility == UserVisibilityLevel.Everyone
                    || (x.PrivacySetting!.Visibility == UserVisibilityLevel.Authenticated && visitorId != null)
                    || (x.PrivacySetting!.Visibility == UserVisibilityLevel.Contacts &&
                      x.Contacts!.Any(y => y.ContactId == visitorId)))
            ? new UserContact(x.Username, x.Profile!.Alias)
            : new UserContact("{ Private }", string.Empty);
    }
}
