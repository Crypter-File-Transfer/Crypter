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

using Crypter.Common.Monads;
using Crypter.Contracts.Features.Contacts;
using Crypter.Core.Entities;
using Crypter.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserContactsService
   {
      Task<GetUserContactsResponse> GetUserContactsAsync(Guid userId, CancellationToken cancellationToken);
      Task<Either<AddUserContactError, AddUserContactResponse>> UpsertUserContactAsync(Guid userId, AddUserContactRequest request, CancellationToken cancellationToken);
      Task<RemoveUserContactResponse> RemoveUserContactAsync(Guid userId, RemoveUserContactRequest request, CancellationToken cancellationToken);
   }

   public class UserContactsService : IUserContactsService
   {
      private readonly DataContext _context;

      public UserContactsService(DataContext context)
      {
         _context = context;
      }

      public async Task<Either<AddUserContactError, AddUserContactResponse>> UpsertUserContactAsync(Guid userId, AddUserContactRequest request, CancellationToken cancellationToken)
      {
         var foundUser = await _context.Users
            .Where(x => x.Username == request.ContactUsername)
            .Where(LinqUserExpressions.UserPrivacyAllowsVisitor(userId))
            .Select(x => new { x.Id, x.Username, x.Profile.Alias })
            .FirstOrDefaultAsync(cancellationToken);

         if (foundUser is null)
         {
            return AddUserContactError.NotFound;
         }

         if (userId == foundUser.Id)
         {
            return AddUserContactError.InvalidUser;
         }

         var existingContact = await _context.UserContacts
            .FirstOrDefaultAsync(x => x.OwnerId == userId && x.ContactId == foundUser.Id, cancellationToken);

         if (existingContact is null)
         {
            UserContactEntity newContactEntity = new UserContactEntity(userId, foundUser.Id);
            _context.UserContacts.Add(newContactEntity);
            await _context.SaveChangesAsync(cancellationToken);
         }

         UserContactDTO dto = new UserContactDTO(foundUser.Username, foundUser.Alias);
         return new AddUserContactResponse(dto);
      }

      public async Task<GetUserContactsResponse> GetUserContactsAsync(Guid userId, CancellationToken cancellationToken)
      {
         var contacts = await _context.UserContacts
            .Where(x => x.OwnerId == userId)
            .Select(x => x.Contact)
            .Select(LinqUserExpressions.ToUserContactDTO(userId))
            .ToListAsync(cancellationToken);

         return new GetUserContactsResponse(contacts);
      }

      public async Task<RemoveUserContactResponse> RemoveUserContactAsync(Guid userId, RemoveUserContactRequest request, CancellationToken cancellationToken)
      {
         var contactEntity = await _context.UserContacts
            .FirstOrDefaultAsync(x => x.OwnerId == userId && x.Contact.Username == request.ContactUsername, cancellationToken);

         if (contactEntity is not null)
         {
            _context.UserContacts.Remove(contactEntity);
            await _context.SaveChangesAsync(cancellationToken);
         }

         return new RemoveUserContactResponse();
      }
   }
}
