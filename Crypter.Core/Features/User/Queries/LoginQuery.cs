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
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication.Login;
using Crypter.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Queries
{
   public class LoginQuery : IRequest<Either<LoginError, LoginQueryResult>>
   {
      public Username Username { get; init; }
      public AuthenticationPassword Password { get; init; }

      public LoginQuery(Username username, AuthenticationPassword password)
      {
         Username = username;
         Password = password;
      }

      public static Either<LoginError, LoginQuery> ValidateFrom(string username, string password)
      {
         if (!Username.TryFrom(username, out var validUsername))
         {
            return LoginError.NotFound;
         }

         if (!AuthenticationPassword.TryFrom(password, out var validAuthenticationPassword))
         {
            return LoginError.NotFound;
         }

         return new LoginQuery(validUsername, validAuthenticationPassword);
      }
   }

   public class LoginQueryResult
   {
      public Guid UserId { get; init; }
      public string Username { get; init; }

      public LoginQueryResult(Guid userId, string username)
      {
         UserId = userId;
         Username = username;
      }
   }

   public class LoginQueryHandler : IRequestHandler<LoginQuery, Either<LoginError, LoginQueryResult>>
   {
      private readonly DataContext _context;
      private readonly IPasswordHashService _passwordHashService;

      public LoginQueryHandler(DataContext context, IPasswordHashService passwordHashService)
      {
         _context = context;
         _passwordHashService = passwordHashService;
      }

      public async Task<Either<LoginError, LoginQueryResult>> Handle(LoginQuery request, CancellationToken cancellationToken)
      {
         string lowerUsername = request.Username.Value.ToLower();
         Models.User user = await _context.Users
            .FirstOrDefaultAsync(x => x.Username.ToLower() == lowerUsername, cancellationToken);

         if (user is null)
         {
            return LoginError.NotFound;
         }

         bool passwordsMatch = _passwordHashService.VerifySecurePasswordHash(request.Password, user.PasswordHash, user.PasswordSalt);
         return passwordsMatch
            ? new LoginQueryResult(user.Id, user.Username)
            : LoginError.NotFound;
      }
   }
}
