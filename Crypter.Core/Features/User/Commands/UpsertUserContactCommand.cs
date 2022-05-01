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
using Crypter.Contracts.Features.User.AddContact;
using Crypter.Core.Entities;
using Crypter.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Commands
{
   public class UpsertUserContactCommand : IRequest<Either<AddUserContactError, UpsertUserContactCommandResult>>
   {
      public Guid User { get; init; }
      public Guid Contact { get; init; }

      public UpsertUserContactCommand(Guid user, Guid contact)
      {
         User = user;
         Contact = contact;
      }
   }

   public class UpsertUserContactCommandResult
   {
      public Guid UserContact { get; init; }

      public UpsertUserContactCommandResult(Guid userContact)
      {
         UserContact = userContact;
      }
   }

   public class UpsertUserContactCommandHandler : IRequestHandler<UpsertUserContactCommand, Either<AddUserContactError, UpsertUserContactCommandResult>>
   {
      private readonly DataContext _context;
      private readonly IUserPrivacyService _userPrivacyService;

      public UpsertUserContactCommandHandler(DataContext context, IUserPrivacyService userPrivacyService)
      {
         _context = context;
         _userPrivacyService = userPrivacyService;
      }

      public async Task<Either<AddUserContactError, UpsertUserContactCommandResult>> Handle(UpsertUserContactCommand request, CancellationToken cancellationToken)
      {
         UserEntity contactUser = await _context.Users
            .Include(x => x.PrivacySetting)
            .Include(x => x.Contacts)
            .FirstOrDefaultAsync(x => x.Id == request.Contact, cancellationToken);

         if (contactUser == default)
         {
            return AddUserContactError.NotFound;
         }

         if (!_userPrivacyService.UserIsVisibleToVisitor(contactUser, request.User))
         {
            return AddUserContactError.NotFound;
         }

         UserContactEntity existingContact = await _context.UserContacts
            .FirstOrDefaultAsync(x => x.OwnerId == request.User && x.ContactId == request.Contact, cancellationToken);

         Guid userContactId = existingContact == default
            ? await AddContactAsync(request.User, request.Contact, cancellationToken)
            : existingContact.Id;

         return new UpsertUserContactCommandResult(userContactId);
      }

      private async Task<Guid> AddContactAsync(Guid user, Guid contact, CancellationToken cancellationToken)
      {
         var newContact = new UserContactEntity(Guid.NewGuid(), user, contact);
         _context.UserContacts.Add(newContact);
         await _context.SaveChangesAsync(cancellationToken);
         return newContact.Id;
      }
   }
}
