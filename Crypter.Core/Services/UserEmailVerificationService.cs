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
using Crypter.Contracts.Features.Settings;
using Crypter.Core.Entities;
using Crypter.Core.Models;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserEmailVerificationService
   {
      Task<Maybe<UserEmailAddressVerificationParameters>> CreateNewVerificationParametersAsync(Guid userId, CancellationToken cancellationToken);
      Task<int> SaveSentVerificationParametersAsync(UserEmailAddressVerificationParameters parameters, CancellationToken cancellationToken);
      Task<Maybe<VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest request, CancellationToken cancellationToken);
   }

   public class UserEmailVerificationService : IUserEmailVerificationService
   {
      private readonly DataContext _context;

      public UserEmailVerificationService(DataContext context)
      {
         _context = context;
      }

      public Task<Maybe<UserEmailVerificationEntity>> GetEntityAsync(Guid userId, CancellationToken cancellationToken)
      {
         var query = _context.UserEmailVerifications
            .FirstOrDefaultAsync(x => x.Owner == userId, cancellationToken);

         return Maybe<UserEmailVerificationEntity>.FromAsync(query);
      }

      public async Task<Maybe<UserEmailAddressVerificationParameters>> CreateNewVerificationParametersAsync(Guid userId, CancellationToken cancellationToken)
      {
         var user = await _context.Users
            .Where(x => x.Id == userId)
            .Where(x => !string.IsNullOrEmpty(x.EmailAddress))
            .Where(x => !x.EmailVerified)
            .Where(x => x.EmailVerification == null)
            .Select(x => new { x.Id, x.EmailAddress })
            .FirstOrDefaultAsync(cancellationToken);

         if (user is null
            || !EmailAddress.TryFrom(user.EmailAddress, out var validEmailAddress))
         {
            return Maybe<UserEmailAddressVerificationParameters>.None;
         }

         Guid verificationCode = Guid.NewGuid();
         AsymmetricKeyPair keyPair = PublicKeyAuth.GenerateKeyPair();

         byte[] verificationCodeBytes = verificationCode.ToByteArray();
         byte[] signature = PublicKeyAuth.Sign(verificationCodeBytes, keyPair.PrivateKey);

         return new UserEmailAddressVerificationParameters(userId, validEmailAddress, verificationCode, signature, keyPair.PublicKey);
      }

      public async Task<int> SaveSentVerificationParametersAsync(UserEmailAddressVerificationParameters parameters, CancellationToken cancellationToken)
      {
         var newEntity = new UserEmailVerificationEntity(parameters.UserId, parameters.VerificationCode, parameters.VerificationKey, DateTime.UtcNow);
         _context.UserEmailVerifications.Add(newEntity);
         return await _context.SaveChangesAsync(cancellationToken);
      }

      public async Task<Maybe<VerifyEmailAddressResponse>> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest request, CancellationToken cancellationToken)
      {
         var verificationCode = EmailVerificationEncoder.DecodeVerificationCodeFromUrlSafe(request.Code);

         var verificationEntity = await _context.UserEmailVerifications
            .FirstOrDefaultAsync(x => x.Code == verificationCode, cancellationToken);

         if (verificationEntity is null)
         {
            return Maybe<VerifyEmailAddressResponse>.None;
         }

         byte[] signature = EmailVerificationEncoder.DecodeSignatureFromUrlSafe(request.Signature);

         if (!PublicKeyAuth.Verify(verificationEntity.Code.ToByteArray(), signature, verificationEntity.VerificationKey))
         {
            return Maybe<VerifyEmailAddressResponse>.None;
         }

         var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == verificationEntity.Owner, cancellationToken);

         if (user is not null)
         {
            user.EmailVerified = true;
         }

         _context.UserEmailVerifications.Remove(verificationEntity);
         await _context.SaveChangesAsync(CancellationToken.None);

         return new VerifyEmailAddressResponse();
      }
   }
}
