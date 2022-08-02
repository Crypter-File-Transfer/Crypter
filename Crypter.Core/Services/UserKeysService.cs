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
using Crypter.Contracts.Features.Authentication;
using Crypter.Contracts.Features.Keys;
using Crypter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserKeysService
   {
      Task<Either<GetUserSeedError, GetUserSeedResponse>> GetUserSeedAsync(Guid userId, CancellationToken cancellationToken);
      Task<Either<GetUserSeedRecoveryProofError, GetUserSeedRecoveryProofResponse>> GetUserSeedProofAsync(Guid userId, GetMasterKeyRecoveryProofRequest request, CancellationToken cancellationToken);
      Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertUserSeedAsync(Guid userId, InsertMasterKeyRequest request, CancellationToken cancellationToken);
      Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertPublicKeyAsync(Guid userId, InsertKeyPairRequest request, CancellationToken cancellationToken);
   }
}

   /*
   public class UserKeysService : IUserKeysService
   {
      private readonly DataContext _context;
      private readonly IUserAuthenticationService _userAuthenticationService;

      public UserKeysService(DataContext context, IUserAuthenticationService userAuthenticationService)
      {
         _context = context;
         _userAuthenticationService = userAuthenticationService;
      }

      public Task<Either<GetUserSeedError, GetUserSeedResponse>> GetUserSeedAsync(Guid userId, CancellationToken cancellationToken)
      {
         return Either<GetUserSeedError, GetUserSeedResponse>.FromRightAsync(
            _context.UserSeeds
               .Where(x => x.Owner == userId)
               .Select(x => new GetMasterKeyResponse(x.Key, x.ClientIV))
               .FirstOrDefaultAsync(cancellationToken), GetUserSeedError.NotFound);
      }

      public async Task<Either<GetUserSeedRecoveryProofError, GetUserSeedRecoveryProofResponse>> GetUserSeedProofAsync(Guid userId, GetMasterKeyRecoveryProofRequest request, CancellationToken cancellationToken)
      {
         var testPasswordResult = await _userAuthenticationService.TestUserPasswordAsync(userId, new TestPasswordRequest(request.Username, request.Password), cancellationToken);
         return await testPasswordResult.MatchAsync<Either<GetUserSeedRecoveryProofError, GetUserSeedRecoveryProofResponse>>(
            error => GetUserSeedRecoveryProofError.InvalidCredentials,
            async _ =>
            {
               var recoveryProof = await _context.UserSeeds
                  .Where(x => x.Owner == userId)
                  .Select(x => x.RecoveryProof)
                  .FirstOrDefaultAsync(cancellationToken);

               return recoveryProof is null
                  ? GetUserSeedRecoveryProofError.NotFound
                  : new GetUserSeedRecoveryProofResponse(recoveryProof);
            },
            GetUserSeedRecoveryProofError.UnknownError);
      }

      public async Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertUserSeedAsync(Guid userId, InsertMasterKeyRequest request, CancellationToken cancellationToken)
      {
         var testPasswordResult = await _userAuthenticationService.TestUserPasswordAsync(userId, new TestPasswordRequest(request.Username, request.Password), cancellationToken);
         return await testPasswordResult.MatchAsync<Either<InsertMasterKeyError, InsertMasterKeyResponse>>(
            error => InsertMasterKeyError.InvalidCredentials,
            async _ =>
            {
               var masterKeyEntity = await _context.UserSeeds
                  .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

               if (masterKeyEntity is not null)
               {
                  return InsertMasterKeyError.Conflict;
               }

               DateTime now = DateTime.UtcNow;
               var newEntity = new UserSeedEntity(userId, request.EncryptedKey, request.ClientIV, request.ClientProof, now, now);
               _context.UserSeeds.Add(newEntity);

               await _context.SaveChangesAsync(cancellationToken);
               return new InsertMasterKeyResponse();
            },
            InsertMasterKeyError.UnknownError);
      }

      public Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetDiffieHellmanPrivateKeyAsync(Guid userId, CancellationToken cancellationToken)
      {
         return Either<GetPrivateKeyError, GetPrivateKeyResponse>.FromRightAsync(
            _context.UserPublicKeys
               .Where(x => x.Owner == userId)
               .Select(x => new GetPrivateKeyResponse(x.PrivateKey, x.ClientIV))
               .FirstOrDefaultAsync(cancellationToken), GetPrivateKeyError.NotFound);
      }

      public Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetDigitalSignaturePrivateKeyAsync(Guid userId, CancellationToken cancellationToken)
      {
         return Either<GetPrivateKeyError, GetPrivateKeyResponse>.FromRightAsync(
            _context.UserEd25519KeyPairs
               .Where(x => x.Owner == userId)
               .Select(x => new GetPrivateKeyResponse(x.PrivateKey, x.ClientIV))
               .FirstOrDefaultAsync(cancellationToken), GetPrivateKeyError.NotFound);
      }

      public async Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertPublicKeyAsync(Guid userId, InsertKeyPairRequest request, CancellationToken cancellationToken)
      {
         var keyPairEntity = await _context.UserPublicKeys
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (keyPairEntity is not null)
         {
            return InsertKeyPairError.Conflict;
         }

         DateTime now = DateTime.UtcNow;
         var newEntity = new UserPublicKeyEntity(userId, request.EncryptedPrivateKey, request.PublicKey, request.ClientIV, now, now);
         _context.UserPublicKeys.Add(newEntity);

         await _context.SaveChangesAsync(cancellationToken);
         return new InsertKeyPairResponse();
      }

      public async Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertDigitalSignatureKeyPairAsync(Guid userId, InsertKeyPairRequest request, CancellationToken cancellationToken)
      {
         var keyPairEntity = await _context.UserEd25519KeyPairs
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (keyPairEntity is not null)
         {
            return InsertKeyPairError.Conflict;
         }

         DateTime now = DateTime.UtcNow;
         var newEntity = new UserEd25519KeyPairEntity(userId, request.EncryptedPrivateKey, request.PublicKey, request.ClientIV, now, now);
         _context.UserEd25519KeyPairs.Add(newEntity);

         await _context.SaveChangesAsync(cancellationToken);
         return new InsertKeyPairResponse();
      }
   }
}
*/