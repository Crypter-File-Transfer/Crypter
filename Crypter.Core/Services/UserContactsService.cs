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

using Crypter.Common.Contracts.Features.Contacts;
using Crypter.Common.Contracts.Features.Contacts.RequestErrorCodes;
using Crypter.Core.Entities;
using Crypter.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyMonads;

namespace Crypter.Core.Services
{
   public interface IUserContactsService
   {
      Task<List<UserContact>> GetUserContactsAsync(Guid userId, CancellationToken cancellationToken = default);
      Task<Either<AddUserContactError, UserContact>> UpsertUserContactAsync(Guid userId, string contactUsername);
      Task<Unit> RemoveUserContactAsync(Guid userId, string contactUsername);
   }

   public class UserContactsService : IUserContactsService
   {
      private readonly DataContext _context;

      public UserContactsService(DataContext context)
      {
         _context = context;
      }

      public async Task<Either<AddUserContactError, UserContact>> UpsertUserContactAsync(Guid userId, string contactUsername)
      {
         string lowerContactUsername = contactUsername.ToLower();

         var foundUser = await _context.Users
            .Where(x => x.Username.ToLower() == lowerContactUsername)
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(userId))
            .Select(x => new { x.Id, x.Username, x.Profile.Alias })
            .FirstOrDefaultAsync();

         if (foundUser is null)
         {
            return AddUserContactError.NotFound;
         }

         if (userId == foundUser.Id)
         {
            return AddUserContactError.InvalidUser;
         }

         bool contactExists = await _context.UserContacts
            .Where(x => x.OwnerId == userId)
            .Where(x => x.ContactId == foundUser.Id)
            .AnyAsync();

         if (!contactExists)
         {
            UserContactEntity newContactEntity = new UserContactEntity(userId, foundUser.Id);
            _context.UserContacts.Add(newContactEntity);
            await _context.SaveChangesAsync();
         }

         return new UserContact(foundUser.Username, foundUser.Alias);
      }

      public Task<List<UserContact>> GetUserContactsAsync(Guid userId, CancellationToken cancellationToken = default)
      {
         return _context.UserContacts
            .Where(x => x.OwnerId == userId)
            .Select(x => x.Contact)
            .Select(LinqUserExpressions.ToUserContactDTO(userId))
            .ToListAsync(cancellationToken);
      }

      public async Task<Unit> RemoveUserContactAsync(Guid userId, string contactUsername)
      {
         string lowerContactUsername = contactUsername.ToLower();

         UserContactEntity contactEntity = await _context.UserContacts
            .FirstOrDefaultAsync(x => x.OwnerId == userId && x.Contact.Username.ToLower() == lowerContactUsername);

         if (contactEntity is not null)
         {
            _context.UserContacts.Remove(contactEntity);
            await _context.SaveChangesAsync();
         }

         return Unit.Default;
      }
   }
}
