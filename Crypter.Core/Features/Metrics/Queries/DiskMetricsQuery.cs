/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.Metrics.Queries
{
   public class DiskMetricsQuery : IRequest<DiskMetricsQueryResult>
   {
   }

   public class DiskMetricsQueryResult
   {
      public long UsedBytes { get; set; }

      public DiskMetricsQueryResult(long usedBytes)
      {
         UsedBytes = usedBytes;
      }
   }

   public class DiskMetricsQueryHandler : IRequestHandler<DiskMetricsQuery, DiskMetricsQueryResult>
   {
      private readonly DataContext _context;

      public DiskMetricsQueryHandler(DataContext context)
      {
         _context = context;
      }

      public async Task<DiskMetricsQueryResult> Handle(DiskMetricsQuery request, CancellationToken cancellationToken)
      {
         var messageSizes = _context.MessageTransfers
            .Select(x => (long)x.Size);

         var fileSizes = _context.FileTransfers
            .Select(x => (long)x.Size);

         long usedBytes = await fileSizes
            .Concat(messageSizes)
            .SumAsync(cancellationToken);

         return new DiskMetricsQueryResult(usedBytes);
      }
   }
}
