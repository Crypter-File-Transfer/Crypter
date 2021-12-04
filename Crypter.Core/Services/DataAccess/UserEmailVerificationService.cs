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

using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserEmailVerificationService : IUserEmailVerificationService
   {
      private readonly DataContext Context;

      public UserEmailVerificationService(DataContext context)
      {
         Context = context;
      }

      public async Task<bool> InsertAsync(Guid userId, Guid code, byte[] verificationKey)
      {
         var emailVerification = new UserEmailVerification(userId, code, verificationKey, DateTime.UtcNow);
         Context.UserEmailVerification.Add(emailVerification);
         await Context.SaveChangesAsync();
         return true;
      }

      public async Task<IUserEmailVerification> ReadAsync(Guid userId)
      {
         return await Context.UserEmailVerification.FindAsync(userId);
      }

      public async Task<IUserEmailVerification> ReadCodeAsync(Guid code)
      {
         return await Context.UserEmailVerification
            .Where(x => x.Code == code)
            .FirstOrDefaultAsync();
      }

      public async Task DeleteAsync(Guid userId)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"UserEmailVerification\" WHERE \"UserEmailVerification\".\"Owner\" = {0}", userId);
      }
   }
}
