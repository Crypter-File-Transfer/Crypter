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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Contracts.Features.UserConsents;
using Crypter.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserConsent.Queries;

public sealed record GetUserConsentsQuery(Guid UserId)
    : IRequest<Dictionary<UserConsentType, DateTimeOffset?>>;

internal sealed class GetUserConsentsQueryHandler
    : IRequestHandler<GetUserConsentsQuery, Dictionary<UserConsentType, DateTimeOffset?>>
{
    private readonly DataContext _dataContext;

    public GetUserConsentsQueryHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<Dictionary<UserConsentType, DateTimeOffset?>> Handle(GetUserConsentsQuery request, CancellationToken cancellationToken)
    {
        Dictionary<UserConsentType, DateTime> activeConsents = await _dataContext.UserConsents
            .Where(x => x.Owner == request.UserId && x.Active)
            .ToDictionaryAsync(x => x.ConsentType, x => x.Activated, cancellationToken);
        
        Dictionary<UserConsentType, DateTimeOffset?> returnMap = Enum.GetValues<UserConsentType>()
            .ToDictionary(x => x, _ => (DateTimeOffset?)null);

        foreach (KeyValuePair<UserConsentType, DateTimeOffset?> entry in returnMap)
        {
            if (activeConsents.TryGetValue(entry.Key, out DateTime consentTime))
            {
                returnMap[entry.Key] = new DateTimeOffset(consentTime, TimeSpan.Zero);
            }
        }

        return returnMap;
    }
}
