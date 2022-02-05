﻿/*
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

using Crypter.Contracts.Common.Enum;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
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

      public async Task InsertAsync(Guid tokenId, Guid userId, string description, TokenType type, DateTime expiration, CancellationToken cancellationToken)
      {
         var token = new UserToken(tokenId, userId, description, type, DateTime.UtcNow, expiration);
         Context.UserTokens.Add(token);
         await Context.SaveChangesAsync(cancellationToken);
      }

      public async Task<IUserToken> ReadAsync(Guid tokenId, CancellationToken cancellationToken)
      {
         return await Context.UserTokens.FindAsync(new object[] { tokenId }, cancellationToken);
      }

      public async Task DeleteAsync(Guid tokenId, CancellationToken cancellationToken)
      {
         await Context.Database
            .ExecuteSqlRawAsync("DELETE FROM \"UserToken\" WHERE \"UserToken\".\"Id\" = {0}", new object[] { tokenId }, cancellationToken);
      }
   }
}
