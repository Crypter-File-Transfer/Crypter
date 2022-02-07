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

using Crypter.Contracts.Common.Enum;
using Crypter.Core.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Commands
{
   public class InsertRefreshTokenCommand : IRequest<Unit>
   {
      public Guid TokenId { get; private set; }
      public Guid UserId { get; private set; }
      public string UserAgent { get; private set; }
      public TokenType TokenType { get; private set; }
      public DateTime TokenExpiration { get; private set; }

      public InsertRefreshTokenCommand(Guid tokenId, Guid userId, string userAgent, TokenType tokenType, DateTime tokenExpiration)
      {
         TokenId = tokenId;
         UserId = userId;
         UserAgent = userAgent;
         TokenType = tokenType;
         TokenExpiration = tokenExpiration;
      }
   }

   public class InsertRefreshTokenCommandHandler : IRequestHandler<InsertRefreshTokenCommand, Unit>
   {
      private readonly DataContext _context;

      public InsertRefreshTokenCommandHandler(DataContext context)
      {
         _context = context;
      }

      public async Task<Unit> Handle(InsertRefreshTokenCommand request, CancellationToken cancellationToken)
      {
         UserToken token = new(request.TokenId, request.UserId, request.UserAgent, request.TokenType, DateTime.UtcNow, request.TokenExpiration);
         _context.UserTokens.Add(token);
         await _context.SaveChangesAsync(cancellationToken);
         return Unit.Value;
      }
   }
}
