﻿/*
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
using Crypter.Contracts.Features.User.GetContacts;
using Crypter.Core.Entities;
using Crypter.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Queries
{
   public class UserContactQuery : IRequest<Maybe<UserContactDTO>>
   {
      public Guid UserId { get; init; }
      public string ContactUsername { get; init; }

      public UserContactQuery(Guid userId, string contactUsername)
      {
         UserId = userId;
         ContactUsername = contactUsername;
      }
   }

   public class UserContactQueryHandler : IRequestHandler<UserContactQuery, Maybe<UserContactDTO>>
   {
      private readonly DataContext _context;
      private readonly IUserPrivacyService _userPrivacyService;

      public UserContactQueryHandler(DataContext context, IUserPrivacyService userPrivacyService)
      {
         _context = context;
         _userPrivacyService = userPrivacyService;
      }

      public async Task<Maybe<UserContactDTO>> Handle(UserContactQuery request, CancellationToken cancellationToken)
      {
         UserContactEntity userContact = await _context.UserContacts
            .Include(x => x.Contact)
               .ThenInclude(x => x.Profile)
            .Include(x => x.Contact)
               .ThenInclude(x => x.PrivacySetting)
            .Include(x => x.Contact)
               .ThenInclude(x => x.Contacts)
            .FirstOrDefaultAsync(x => x.Contact.Username == request.ContactUsername, cancellationToken);

         if (userContact == default)
         {
            return null;
         }

         var contactDTO = _userPrivacyService.UserIsVisibleToVisitor(userContact.Contact, request.UserId)
            ? new UserContactDTO(userContact.Contact.Username, userContact.Contact.Profile.Alias)
            : new UserContactDTO("{ Private }");

         return contactDTO;
      }
   }
}
