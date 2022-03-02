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

using Crypter.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Commands
{
   public class RemoveUserContactCommand : IRequest<Unit>
   {
      public Guid User { get; private set; }
      public Guid Contact { get; private set; }

      public RemoveUserContactCommand(Guid user, Guid contact)
      {
         User = user;
         Contact = contact;
      }
   }

   public class RemoveUserContactCommandHandler : IRequestHandler<RemoveUserContactCommand, Unit>
   {
      private readonly DataContext _context;

      public RemoveUserContactCommandHandler(DataContext context)
      {
         _context = context;
      }

      public async Task<Unit> Handle(RemoveUserContactCommand request, CancellationToken cancellationToken)
      {
         UserContact contact = await _context.UserContacts
            .FirstOrDefaultAsync(x => x.OwnerId == request.User && x.ContactId == request.Contact, cancellationToken);

         if (contact != default)
         {
            _context.Remove(contact);
            await _context.SaveChangesAsync(cancellationToken);
         }

         return Unit.Value;
      }
   }
}
