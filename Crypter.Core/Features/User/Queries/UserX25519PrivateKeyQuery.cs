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
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Queries
{
   public class UserX25519PrivateKeyQuery : IRequest<Maybe<UserX25519PPrivateKeyQueryResult>>
   {
      public Guid User { get; init; }

      public UserX25519PrivateKeyQuery(Guid user)
      {
         User = user;
      }
   }

   public class UserX25519PPrivateKeyQueryResult
   {
      public string EncryptedPrivateKey { get; init; }
      public string IV { get; init; }

      public UserX25519PPrivateKeyQueryResult(string encryptedPrivateKey, string iv)
      {
         EncryptedPrivateKey = encryptedPrivateKey;
         IV = iv;
      }
   }

   public class UserX25519PrivateKeyQueryHandler : IRequestHandler<UserX25519PrivateKeyQuery, Maybe<UserX25519PPrivateKeyQueryResult>>
   {
      private readonly DataContext _context;

      public UserX25519PrivateKeyQueryHandler(DataContext context)
      {
         _context = context;
      }

      public async Task<Maybe<UserX25519PPrivateKeyQueryResult>> Handle(UserX25519PrivateKeyQuery request, CancellationToken cancellationToken)
      {
         var result = await _context.UserX25519KeyPairs
            .Where(x => x.Owner == request.User)
            .Select(x => new UserX25519PPrivateKeyQueryResult(x.PrivateKey, x.ClientIV))
            .FirstOrDefaultAsync(cancellationToken);

         return result == default
            ? Maybe<UserX25519PPrivateKeyQueryResult>.None
            : new Maybe<UserX25519PPrivateKeyQueryResult>(result);
      }
   }
}
