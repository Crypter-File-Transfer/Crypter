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
using Crypter.Common.Contracts.Features.Setting;
using Crypter.Common.Enums;
using Crypter.DataAccess;
using EasyMonads;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.Setting.Queries;

public sealed record GetUploadSettingsQuery(Maybe<Guid> PossibleUserId)
    : IRequest<UploadSettings>;

public sealed class GetUploadSettingsQueryHandler : IRequestHandler<GetUploadSettingsQuery, UploadSettings>
{
    private readonly DataContext _dataContext;
    
    public GetUploadSettingsQueryHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }
    
    public async Task<UploadSettings> Handle(GetUploadSettingsQuery request, CancellationToken cancellationToken)
    {
        Guid? userId = request.PossibleUserId
            .Bind<Guid?>(x => x)
            .SomeOrDefault(null);

        return await _dataContext.DataTiers
            .Where(x => userId == null && x.DefaultForUserCategory == UserCategory.Anonymous
                        || (_dataContext.Users.Any(y => y.Id == userId && string.IsNullOrEmpty(y.EmailAddress)) && x.DefaultForUserCategory == UserCategory.Authenticated)
                        || _dataContext.Users.Any(y => y.Id == userId && !string.IsNullOrEmpty(y.EmailAddress) && x.DefaultForUserCategory == UserCategory.Verified))
            .Select(x => new UploadSettings(
                x.MaxSingleUploadSize,
                x.DefaultForUserCategory == UserCategory.Anonymous
                    ? 0
                    : _dataContext.UserFileTransfers
                          .Where(y => y.RecipientId == userId || y.SenderId == userId && y.RecipientId == null)
                          .Select(y => y.Size)
                          .Sum()
                      + _dataContext.UserMessageTransfers
                          .Where(y => y.RecipientId == userId || y.SenderId == userId && y.RecipientId == null)
                          .Select(y => y.Size)
                          .Sum(),
                x.MaxTotalStorageSize))
            .FirstOrDefaultAsync(cancellationToken) ?? new UploadSettings(0, 0, 0);
    }
}
