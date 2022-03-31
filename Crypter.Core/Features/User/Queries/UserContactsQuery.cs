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

using Crypter.Contracts.Features.User.GetContacts;
using Crypter.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Queries
{
   public class UserContactsQuery : IRequest<IEnumerable<UserContactDTO>>
   {
      public Guid User { get; init; }

      public UserContactsQuery(Guid user)
      {
         User = user;
      }
   }

   public class UserContactsQueryHandler : IRequestHandler<UserContactsQuery, IEnumerable<UserContactDTO>>
   {
      private readonly DataContext _context;
      private readonly IUserPrivacyService _userPrivacyService;

      public UserContactsQueryHandler(DataContext context, IUserPrivacyService userPrivacyService)
      {
         _context = context;
         _userPrivacyService = userPrivacyService;
      }

      public async Task<IEnumerable<UserContactDTO>> Handle(UserContactsQuery request, CancellationToken cancellationToken)
      {
         List<Models.UserContact> contacts = await _context.UserContacts
            .Where(x => x.OwnerId == request.User)
            .Include(x => x.Contact)
               .ThenInclude(x => x.Profile)
            .Include(x => x.Contact)
               .ThenInclude(x => x.PrivacySetting)
            .Include(x => x.Contact)
               .ThenInclude(x => x.Contacts)
            .ToListAsync(cancellationToken);

         return contacts
            .Select(x =>
            {
               return _userPrivacyService.UserIsVisibleToVisitor(x.Contact, request.User)
                  ? new UserContactDTO(x.ContactId, x.Contact.Username, x.Contact.Profile.Alias)
                  : new UserContactDTO(x.ContactId, "{ Private }", null);
            })
            .ToList();
      }
   }
}
