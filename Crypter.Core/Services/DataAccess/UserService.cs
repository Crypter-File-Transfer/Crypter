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

using Crypter.Contracts.Enum;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class UserService : IUserService
   {
      private readonly DataContext Context;

      public UserService(DataContext context)
      {
         Context = context;
      }

      public async Task<Guid> InsertAsync(string username, string password, string email, CancellationToken cancellationToken)
      {
         (var passwordKey, var passwordHash) = PasswordHashService.MakeSecurePasswordHash(password);

         var user = new User(
             Guid.NewGuid(),
             username,
             email,
             passwordHash,
             passwordKey,
             false,
             DateTime.UtcNow,
             DateTime.MinValue);
         Context.Users.Add(user);

         await Context.SaveChangesAsync(cancellationToken);
         return user.Id;
      }

      public async Task<IUser> ReadAsync(Guid id, CancellationToken cancellationToken)
      {
         return await Context.Users.FindAsync(new object[] { id }, cancellationToken);
      }

      public async Task<IUser> ReadAsync(string username, CancellationToken cancellationToken)
      {
         return await Context.Users
            .Where(user => user.Username.ToLower() == username.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
      }

      public async Task<UpdateContactInfoResult> UpdateContactInfoAsync(Guid id, string email, string currentPassword, CancellationToken cancellationToken)
      {
         var user = await ReadAsync(id, cancellationToken);
         var passwordsMatch = PasswordHashService.VerifySecurePasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt);
         if (!passwordsMatch)
         {
            return UpdateContactInfoResult.PasswordValidationFailed;
         }

         user.Email = email;
         user.EmailVerified = false;
         await Context.SaveChangesAsync(cancellationToken);
         return UpdateContactInfoResult.Success;
      }

      public async Task UpdateEmailAddressVerification(Guid id, bool isVerified, CancellationToken cancellationToken)
      {
         var user = await ReadAsync(id, cancellationToken);
         user.EmailVerified = isVerified;
         await Context.SaveChangesAsync();
      }

      public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"Users\" WHERE \"Users\".\"Id\" = {0}", new object[] { id }, cancellationToken);
      }

      public async Task<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken)
      {
         string lowerUsername = username.ToLower();
         return !await Context.Users.AnyAsync(x => x.Username.ToLower() == lowerUsername, cancellationToken);
      }

      public async Task<bool> IsEmailAddressAvailableAsync(string email, CancellationToken cancellationToken)
      {
         string lowerEmail = email.ToLower();
         return !await Context.Users.AnyAsync(x => x.Email.ToLower() == lowerEmail, cancellationToken);
      }
   }
}
