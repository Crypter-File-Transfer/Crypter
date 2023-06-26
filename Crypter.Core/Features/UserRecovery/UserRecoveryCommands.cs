/*
 * Copyright (C) 2023 Crypter File Transfer
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

using Crypter.Common.Contracts.Features.UserRecovery.SubmitRecovery;
using Crypter.Common.Infrastructure;
using Crypter.Common.Primitives;
using Crypter.Core.Entities;
using Crypter.Core.Features.Keys;
using Crypter.Core.Features.UserRecovery.Models;
using Crypter.Core.Identity;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Crypto.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using EasyMonads;

namespace Crypter.Core.Features.UserRecovery
{
   internal static class UserRecoveryCommands
   {
      internal static Task SaveRecoveryParametersAsync(DataContext dataContext, UserRecoveryParameters userRecoveryParameters, DateTime created)
      {
         UserRecoveryEntity newEntity = new UserRecoveryEntity(userRecoveryParameters.UserId, userRecoveryParameters.RecoveryCode, userRecoveryParameters.VerificationKey, created);
         dataContext.UserRecoveries.Add(newEntity);
         return dataContext.SaveChangesAsync();
      }

      internal static async Task DeleteRecoveryParametersAsync(DataContext dataContext, Guid userId)
      {
         UserRecoveryEntity savedEntity = await dataContext.UserRecoveries
            .FirstOrDefaultAsync(x => x.Owner == userId);

         if (savedEntity is not null)
         {
            dataContext.UserRecoveries.Remove(savedEntity);
            await dataContext.SaveChangesAsync();
         }
      }

      internal static async Task<Either<SubmitRecoveryError, PerformRecoveryResult>> PerformRecoveryAsync(DataContext dataContext, ICryptoProvider cryptoProvider, IPasswordHashService passwordHashService, SubmitRecoveryRequest recoveryRequest)
      {
         if (!Username.TryFrom(recoveryRequest.Username, out var validUsername))
         {
            return SubmitRecoveryError.InvalidUsername;
         }

         UserRecoveryEntity recoveryEntity = await dataContext.UserRecoveries
            .Where(x => x.User.Username == validUsername.Value)
            .FirstOrDefaultAsync();

         if (recoveryEntity is null)
         {
            return SubmitRecoveryError.RecoveryNotFound;
         }

         bool validRecoveryProofProvided = false;
         if (recoveryRequest.ReplacementMasterKeyInformation?.CurrentRecoveryProof?.Length > 0)
         {
            if (!MasterKeyValidators.ValidateMasterKeyInformation(recoveryRequest.ReplacementMasterKeyInformation?.EncryptedKey, recoveryRequest.ReplacementMasterKeyInformation?.Nonce, recoveryRequest.ReplacementMasterKeyInformation?.NewRecoveryProof))
            {
               return SubmitRecoveryError.InvalidMasterKey;
            }

            validRecoveryProofProvided = await dataContext.UserMasterKeys
               .Where(x => x.Owner == recoveryEntity.Owner)
               .Where(x => x.RecoveryProof == recoveryRequest.ReplacementMasterKeyInformation.CurrentRecoveryProof)
               .AnyAsync();

            if (!validRecoveryProofProvided)
            {
               return SubmitRecoveryError.WrongRecoveryKey;
            }
         }

         Guid decodedRecoveryCode = UrlSafeEncoder.DecodeGuidFromUrlSafe(recoveryRequest.RecoveryCode);
         byte[] decodedSignature = UrlSafeEncoder.DecodeBytesFromUrlSafe(recoveryRequest.RecoverySignature);

         bool codesMatch = Common.RecoveryCodesMatch(cryptoProvider, recoveryEntity.Code, decodedRecoveryCode);
         bool validSignature = Common.VerifyRecoverySignature(cryptoProvider, recoveryEntity.VerificationKey, decodedRecoveryCode, validUsername, decodedSignature);

         Either<SubmitRecoveryError, PerformRecoveryResult> result = codesMatch && validSignature
            ? new PerformRecoveryResult(recoveryEntity.Owner, false, false)
            : SubmitRecoveryError.UnknownError;

         await result.DoRightAsync(async recoveryResult =>
         {
            UserEntity user = await dataContext.Users
               .Include(x => x.MasterKey)
               .Where(x => x.Id == recoveryEntity.Owner)
               .FirstAsync();

            DateTime utcNow = DateTime.UtcNow;
            if (validRecoveryProofProvided)
            {
               user.MasterKey.EncryptedKey = recoveryRequest.ReplacementMasterKeyInformation.EncryptedKey;
               user.MasterKey.Nonce = recoveryRequest.ReplacementMasterKeyInformation.Nonce;
               user.MasterKey.RecoveryProof = recoveryRequest.ReplacementMasterKeyInformation.NewRecoveryProof;
               user.MasterKey.Updated = utcNow;
            }
            else
            {
               recoveryResult.DeleteUserReceivedTransfers = true;
               recoveryResult.DeleteUserKeys = true;
            }

            UserConsentEntity latestRecoveryConsent = await dataContext.UserConsents
               .Where(x => x.Owner == user.Id)
               .Where(x => x.ConsentType == ConsentType.RecoveryKeyRisks)
               .OrderBy(x => x.Created)
               .FirstOrDefaultAsync();

            if (latestRecoveryConsent is not null)
            {
               latestRecoveryConsent.Active = false;
               latestRecoveryConsent.Deactivated = utcNow;
            }

            SecurePasswordHashOutput securePasswordData = passwordHashService.MakeSecurePasswordHash(recoveryRequest.VersionedPassword.Password, passwordHashService.LatestServerPasswordVersion);

            user.PasswordHash = securePasswordData.Hash;
            user.PasswordSalt = securePasswordData.Salt;
            user.ServerPasswordVersion = passwordHashService.LatestServerPasswordVersion;
            user.ClientPasswordVersion = recoveryRequest.VersionedPassword.Version;

            dataContext.UserRecoveries.Remove(recoveryEntity);

            await dataContext.SaveChangesAsync();
         });

         return result;
      }
   }
}
