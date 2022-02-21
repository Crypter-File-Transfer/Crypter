/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserTokenService : IUserTokenService
   {
      private readonly DataContext Context;

      public UserTokenService(DataContext context)
      {
         Context = context;
      }

      /// <summary>
      /// Delete a token from the UserToken table
      /// </summary>
      /// <param name="tokenId"></param>
      /// <param name="cancellationToken"></param>
      /// <returns></returns>
      /// <remarks>
      /// Do not remove or modify until all scheduled Hangfire jobs have completed.
      /// </remarks>
      public async Task DeleteAsync(Guid tokenId, CancellationToken cancellationToken)
      {
         await Context.Database
            .ExecuteSqlRawAsync("DELETE FROM \"UserToken\" WHERE \"UserToken\".\"Id\" = {0}", new object[] { tokenId }, cancellationToken);
      }
   }
}
