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
using Crypter.Contracts.Features.Authentication;
using Crypter.Contracts.Features.Keys;
using Crypter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserKeysService
   {
      Task<Either<GetMasterKeyError, GetMasterKeyResponse>> GetMasterKeyAsync(Guid userId, CancellationToken cancellationToken);
      Task<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>> GetMasterKeyProofAsync(Guid userId, GetMasterKeyRecoveryProofRequest request, CancellationToken cancellationToken);
      Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertMasterKeyAsync(Guid userId, InsertMasterKeyRequest request, CancellationToken cancellationToken);
      Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetDiffieHellmanPrivateKeyAsync(Guid userId, CancellationToken cancellationToken);
      Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetDigitalSignaturePrivateKeyAsync(Guid userId, CancellationToken cancellationToken);
      Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertDiffieHellmanKeyPairAsync(Guid userId, InsertKeyPairRequest request, CancellationToken cancellationToken);
      Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertDigitalSignatureKeyPairAsync(Guid userId, InsertKeyPairRequest request, CancellationToken cancellationToken);
   }

   public class UserKeysService : IUserKeysService
   {
      private readonly DataContext _context;
      private readonly IUserAuthenticationService _userAuthenticationService;

      public UserKeysService(DataContext context, IUserAuthenticationService userAuthenticationService)
      {
         _context = context;
         _userAuthenticationService = userAuthenticationService;
      }

      public Task<Either<GetMasterKeyError, GetMasterKeyResponse>> GetMasterKeyAsync(Guid userId, CancellationToken cancellationToken)
      {
         return Either<GetMasterKeyError, GetMasterKeyResponse>.FromRightAsync(
            _context.UserMasterKeys
               .Where(x => x.Owner == userId)
               .Select(x => new GetMasterKeyResponse(x.Key, x.ClientIV))
               .FirstOrDefaultAsync(cancellationToken), GetMasterKeyError.NotFound);
      }

      public async Task<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>> GetMasterKeyProofAsync(Guid userId, GetMasterKeyRecoveryProofRequest request, CancellationToken cancellationToken)
      {
         var testPasswordResult = await _userAuthenticationService.TestUserPasswordAsync(userId, new TestPasswordRequest(request.Username, request.Password), cancellationToken);
         return await testPasswordResult.MatchAsync<Either<GetMasterKeyRecoveryProofError, GetMasterKeyRecoveryProofResponse>>(
            error => GetMasterKeyRecoveryProofError.InvalidCredentials,
            async _ =>
            {
               var recoveryProof = await _context.UserMasterKeys
                  .Where(x => x.Owner == userId)
                  .Select(x => x.RecoveryProof)
                  .FirstOrDefaultAsync(cancellationToken);

               return recoveryProof is null
                  ? GetMasterKeyRecoveryProofError.NotFound
                  : new GetMasterKeyRecoveryProofResponse(recoveryProof);
            },
            GetMasterKeyRecoveryProofError.UnknownError);
      }

      public async Task<Either<InsertMasterKeyError, InsertMasterKeyResponse>> InsertMasterKeyAsync(Guid userId, InsertMasterKeyRequest request, CancellationToken cancellationToken)
      {
         var testPasswordResult = await _userAuthenticationService.TestUserPasswordAsync(userId, new TestPasswordRequest(request.Username, request.Password), cancellationToken);
         return await testPasswordResult.MatchAsync<Either<InsertMasterKeyError, InsertMasterKeyResponse>>(
            error => InsertMasterKeyError.InvalidCredentials,
            async _ =>
            {
               var masterKeyEntity = await _context.UserMasterKeys
                  .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

               if (masterKeyEntity is not null)
               {
                  return InsertMasterKeyError.Conflict;
               }

               DateTime now = DateTime.UtcNow;
               var newEntity = new UserMasterKeyEntity(userId, request.EncryptedKey, request.ClientIV, request.ClientProof, now, now);
               _context.UserMasterKeys.Add(newEntity);

               await _context.SaveChangesAsync(cancellationToken);
               return new InsertMasterKeyResponse();
            },
            InsertMasterKeyError.UnknownError);
      }

      public Task<Either<GetPrivateKeyError, GetPrivateKeyResponse>> GetDiffieHellmanPrivateKeyAsync(Guid userId, CancellationToken cancellationToken)
      {
         return Either<GetPrivateKeyError, GetPrivateKeyResponse>.FromRightAsync(
            _context.UserX25519KeyPairs
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

      public async Task<Either<InsertKeyPairError, InsertKeyPairResponse>> InsertDiffieHellmanKeyPairAsync(Guid userId, InsertKeyPairRequest request, CancellationToken cancellationToken)
      {
         var keyPairEntity = await _context.UserX25519KeyPairs
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         if (keyPairEntity is not null)
         {
            return InsertKeyPairError.Conflict;
         }

         DateTime now = DateTime.UtcNow;
         var newEntity = new UserX25519KeyPairEntity(userId, request.EncryptedPrivateKey, request.PublicKey, request.ClientIV, now, now);
         _context.UserX25519KeyPairs.Add(newEntity);

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
