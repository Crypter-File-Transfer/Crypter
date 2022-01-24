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

using Crypter.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Queries
{
   public class LoginQuery : IRequest<LoginQueryResult>
   {
      public string Username { get; private set; }
      public string Password { get; private set; }

      public LoginQuery(string username, string password)
      {
         Username = username;
         Password = password;
      }
   }

   public class LoginQueryResult
   {
      public bool Success { get; private set; }
      public Guid UserId { get; private set; }
      public string? EncryptedX25519PrivateKey { get; private set; }
      public string? EncryptedEd25519PrivateKey { get; private set; }
      public string? InitVectorX25519 { get; private set; }
      public string? InitVectorEd25519 { get; private set; }

      /// <summary>
      /// Failure
      /// </summary>
      public LoginQueryResult()
      {
         Success = false;
         UserId = Guid.Empty;
      }

      /// <summary>
      /// Success
      /// </summary>
      /// <param name="success"></param>
      /// <param name="userId"></param>
      /// <param name="encryptedX25519PrivateKey"></param>
      /// <param name="encryptedEd25519PrivateKey"></param>
      /// <param name="initVectorX25519"></param>
      /// <param name="initVectorEd25519"></param>
      public LoginQueryResult(Guid userId, string? encryptedX25519PrivateKey, string? encryptedEd25519PrivateKey, string? initVectorX25519, string? initVectorEd25519)
      {
         Success = true;
         UserId = userId;
         EncryptedX25519PrivateKey = encryptedX25519PrivateKey;
         EncryptedEd25519PrivateKey = encryptedEd25519PrivateKey;
         InitVectorX25519 = initVectorX25519;
         InitVectorEd25519 = initVectorEd25519;
      }
   }

   public class LoginQueryHandler : IRequestHandler<LoginQuery, LoginQueryResult>
   {
      private readonly DataContext _context;

      public LoginQueryHandler(DataContext context)
      {
         _context = context;
      }

      public async Task<LoginQueryResult> Handle(LoginQuery request, CancellationToken cancellationToken)
      {
         string lowerUsername = request.Username.ToLower();
         Models.User? user = await _context.Users
            .Include(x => x.X25519KeyPair)
            .Include(x => x.Ed25519KeyPair)
            .FirstOrDefaultAsync(x => x.Username.ToLower() == lowerUsername, cancellationToken);

         if (user is null)
         {
            return new LoginQueryResult();
         }

         bool passwordsMatch = PasswordHashService.VerifySecurePasswordHash(request.Password, user.PasswordHash, user.PasswordSalt);
         return passwordsMatch
            ? new LoginQueryResult(user.Id, user.X25519KeyPair?.PrivateKey, user.Ed25519KeyPair?.PrivateKey, user.X25519KeyPair?.ClientIV, user.Ed25519KeyPair?.ClientIV)
            : new LoginQueryResult();
      }
   }
}
