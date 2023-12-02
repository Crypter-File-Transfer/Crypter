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
using Crypter.Core.Settings;
using Crypter.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crypter.Core.Features.Metrics.Queries;

public record GetDiskMetricsQuery : IRequest<GetDiskMetricsResult>;

public record GetDiskMetricsResult(long AllocatedBytes, long FreeBytes);

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class GetDiskMetricsQueryHandler : IRequestHandler<GetDiskMetricsQuery, GetDiskMetricsResult>
{
    private readonly DataContext _dataContext;
    private readonly TransferStorageSettings _transferStorageSettings;

    public GetDiskMetricsQueryHandler(DataContext dataContext, IOptions<TransferStorageSettings> transferStorageSettings)
    {
        _dataContext = dataContext;
        _transferStorageSettings = transferStorageSettings.Value;
    }
    
    public async Task<GetDiskMetricsResult> Handle(GetDiskMetricsQuery request, CancellationToken cancellationToken)
    {
        return await RunQueryAsync(_dataContext, _transferStorageSettings, cancellationToken);
    }

    public static async Task<GetDiskMetricsResult> RunQueryAsync(DataContext dataContext,
        TransferStorageSettings transferStorageSettings, CancellationToken cancellationToken = default)
    {
        IQueryable<long> anonymousMessageSizes = dataContext.AnonymousMessageTransfers
            .Select(x => x.Size);

        IQueryable<long> userMessageSizes = dataContext.UserMessageTransfers
            .Select(x => x.Size);

        IQueryable<long> anonymousFileSizes = dataContext.AnonymousFileTransfers
            .Select(x => x.Size);

        IQueryable<long> userFileSizes = dataContext.UserFileTransfers
            .Select(x => x.Size);

        long usedBytes = await anonymousMessageSizes
            .Concat(userMessageSizes)
            .Concat(anonymousFileSizes)
            .Concat(userFileSizes)
            .SumAsync(cancellationToken);

        long allocatedBytes = transferStorageSettings.AllocatedGB * Convert.ToInt64(Math.Pow(2, 30));
        long freeBytes = allocatedBytes - usedBytes;

        return new GetDiskMetricsResult(allocatedBytes, freeBytes);
    }
}
