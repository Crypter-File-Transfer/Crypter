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

using Crypter.Common.Primitives;
using Crypter.Contracts.Features.User.UpdateContactInfo;
using Crypter.Core.Entities;
using Crypter.Core.Interfaces;
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
      private readonly IPasswordHashService _passwordHashService;

      public UserService(DataContext context, IPasswordHashService passwordHashService)
      {
         Context = context;
         _passwordHashService = passwordHashService;
      }

      public async Task<User> ReadAsync(Guid id, CancellationToken cancellationToken)
      {
         return await Context.Users.FindAsync(new object[] { id }, cancellationToken);
      }

      public async Task<User> ReadAsync(string username, CancellationToken cancellationToken)
      {
         return await Context.Users
            .Where(user => user.Username.ToLower() == username.ToLower())
            .FirstOrDefaultAsync(cancellationToken);
      }

      public async Task<(bool Success, UpdateContactInfoError Error)> UpdateContactInfoAsync(Guid id, EmailAddress emailAddress, AuthenticationPassword currentPassword, CancellationToken cancellationToken)
      {
         var user = await ReadAsync(id, cancellationToken);
         var passwordsMatch = _passwordHashService.VerifySecurePasswordHash(currentPassword, user.PasswordHash, user.PasswordSalt);
         if (!passwordsMatch)
         {
            return (false, UpdateContactInfoError.PasswordValidationFailed);
         }

         user.Email = emailAddress.Value;
         user.EmailVerified = false;
         await Context.SaveChangesAsync(cancellationToken);
         return (true, UpdateContactInfoError.UnknownError);
      }

      public async Task UpdateEmailAddressVerification(Guid id, bool isVerified, CancellationToken cancellationToken)
      {
         var user = await ReadAsync(id, cancellationToken);
         user.EmailVerified = isVerified;
         await Context.SaveChangesAsync(cancellationToken);
      }

      public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
      {
         await Context.Database
             .ExecuteSqlRawAsync("DELETE FROM \"Users\" WHERE \"Users\".\"Id\" = {0}", new object[] { id }, cancellationToken);
      }
   }
}
