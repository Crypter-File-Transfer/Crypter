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
using Crypter.Contracts.Features.User.GetPublicProfile;
using Crypter.Core.Extensions;
using Crypter.Core.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Features.User.Queries
{
   public class UserProfileQuery : IRequest<Either<GetUserProfileError, GetUserProfileResponse>>
   {
      public Guid RequestorId { get; private set; }
      public string Username { get; private set; }

      public UserProfileQuery(Guid requestorId, string username)
      {
         RequestorId = requestorId;
         Username = username;
      }
   }

   public class UserProfileQueryHandler : IRequestHandler<UserProfileQuery, Either<GetUserProfileError, GetUserProfileResponse>>
   {
      private readonly DataContext _context;
      private readonly IUserPrivacyService _userPrivacyService;

      public UserProfileQueryHandler(DataContext context, IUserPrivacyService userPrivateService)
      {
         _context = context;
         _userPrivacyService = userPrivateService;
      }

      public async Task<Either<GetUserProfileError, GetUserProfileResponse>> Handle(UserProfileQuery request, CancellationToken cancellationToken)
      {
         string lowerUsername = request.Username.ToLower();

         Models.User user = await _context.Users
            .Where(x => x.Username.ToLower() == lowerUsername)
            .Include(x => x.Profile)
            .Include(x => x.PrivacySetting)
            .Include(x => x.Contacts)
            .Include(x => x.X25519KeyPair)
            .Include(x => x.Ed25519KeyPair)
            .Where(LinqExtensions.UserProfileIsComplete())
            .FirstOrDefaultAsync(cancellationToken);

         if (user == default)
         {
            return GetUserProfileError.NotFound;
         }

         var userAllowsVisitorToViewProfile = _userPrivacyService.UserIsVisibleToVisitor(user, request.RequestorId);
         if (!userAllowsVisitorToViewProfile)
         {
            return GetUserProfileError.NotFound;
         }

         var visitorCanSendMessages = _userPrivacyService.UserAcceptsMessageTransfersFromVisitor(user, request.RequestorId);
         var visitorCanSendFiles = _userPrivacyService.UserAcceptsFileTransfersFromVisitor(user, request.RequestorId);

         return new GetUserProfileResponse(user.Id, user.Username, user.Profile.Alias, user.Profile.About,
            user.PrivacySetting.AllowKeyExchangeRequests, visitorCanSendMessages, visitorCanSendFiles,
            user.Ed25519KeyPair.PublicKey, user.X25519KeyPair.PublicKey);
      }
   }
}
