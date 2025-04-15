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
using Crypter.Common.Contracts.Features.UserSettings.TransferSettings;
using Crypter.Common.Enums;
using Crypter.Core.MediatorMonads;
using Crypter.DataAccess;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserSettings.Queries;

public sealed record TransferSettingsQuery(Maybe<Guid> PossibleUserId)
    : IEitherRequest<GetTransferSettingsError, GetTransferSettingsResponse>;

public sealed class TransferSettingsQueryHandler : IEitherRequestHandler<TransferSettingsQuery, GetTransferSettingsError, GetTransferSettingsResponse>
{
    private readonly DataContext _dataContext;

    public TransferSettingsQueryHandler(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<Either<GetTransferSettingsError, GetTransferSettingsResponse>> Handle(TransferSettingsQuery request, CancellationToken cancellationToken)
    {
        Guid? userId = request.PossibleUserId
            .Bind<Guid?>(x => x)
            .SomeOrDefault(null);

        var data = await _dataContext.TransferTiers
            .Where(x => userId == null && x.DefaultForUserCategory == UserCategory.Anonymous
                        || (_dataContext.Users.Any(y => y.Id == userId && string.IsNullOrEmpty(y.EmailAddress)) && x.DefaultForUserCategory == UserCategory.Authenticated)
                        || _dataContext.Users.Any(y => y.Id == userId && !string.IsNullOrEmpty(y.EmailAddress) && x.DefaultForUserCategory == UserCategory.Verified))
            .Select(x => new
            {
                x.Name,
                x.MaximumUploadSize,
                x.UserQuota,
                UsedUserSpace = x.DefaultForUserCategory == UserCategory.Anonymous
                    ? _dataContext.AnonymousFileTransfers
                          .Select(y => y.Size)
                          .Sum()
                      + _dataContext.AnonymousMessageTransfers
                          .Select(y => y.Size)
                          .Sum()
                    : _dataContext.UserFileTransfers
                          .Where(y => y.RecipientId == userId || y.SenderId == userId && y.RecipientId == null)
                          .Select(y => y.Size)
                          .Sum()
                      + _dataContext.UserMessageTransfers
                          .Where(y => y.RecipientId == userId || y.SenderId == userId && y.RecipientId == null)
                          .Select(y => y.Size)
                          .Sum(),
                FreeTransferQuota = _dataContext.ApplicationSettings.Select(y => y.FreeTransferQuota).FirstOrDefault(),
                UsedFreeTransferSpace = _dataContext.AnonymousFileTransfers
                                            .Select(y => y.Size)
                                            .Sum()
                                        + _dataContext.AnonymousMessageTransfers
                                            .Select(y => y.Size)
                                            .Sum()
                                        + _dataContext.UserFileTransfers
                                            .Select(y => y.Size)
                                            .Sum()
                                        + _dataContext.UserMessageTransfers
                                            .Select(y => y.Size)
                                            .Sum()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            return GetTransferSettingsError.TransferTierNotFound;
        }

        return new GetTransferSettingsResponse(data.Name, data.MaximumUploadSize, data.UserQuota - data.UsedUserSpace, data.UsedUserSpace, data.UserQuota, data.FreeTransferQuota - data.UsedFreeTransferSpace, data.UsedFreeTransferSpace, data.FreeTransferQuota);
    }
}
