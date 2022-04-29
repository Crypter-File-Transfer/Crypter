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

using Crypter.Core.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Commands
{
   public class DeleteUserTokenCommand : IRequest<Unit>
   {
      public Guid TokenId { get; private set; }

      public DeleteUserTokenCommand(Guid tokenId)
      {
         TokenId = tokenId;
      }
   }

   public class DeleteUserTokenCommandHandler : IRequestHandler<DeleteUserTokenCommand, Unit>
   {
      private readonly DataContext _context;

      public DeleteUserTokenCommandHandler(DataContext context)
      {
         _context = context;
      }

      public async Task<Unit> Handle(DeleteUserTokenCommand request, CancellationToken cancellationToken)
      {
         UserToken foundToken = await _context.UserTokens
            .FindAsync(new object[] { request.TokenId }, cancellationToken);

         if (foundToken is not null)
         {
            _context.UserTokens.Remove(foundToken);
            await _context.SaveChangesAsync(cancellationToken);
         }

         return Unit.Value;
      }
   }
}
