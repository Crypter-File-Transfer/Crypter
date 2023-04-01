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

using Crypter.Common.Contracts.Features.Settings;
using Crypter.Common.Infrastructure;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Core.Entities;
using Crypter.Core.Features.UserEmailVerification;
using Crypter.Core.Models;
using Crypter.Crypto.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserEmailVerificationService
   {
      Task<Maybe<UserEmailAddressVerificationParameters>> GenerateVerificationParametersAsync(Guid userId);
      Task SaveVerificationParametersAsync(UserEmailAddressVerificationParameters parameters);
      Task<bool> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest request);
   }

   public class UserEmailVerificationService : IUserEmailVerificationService
   {
      private readonly DataContext _dataContext;
      private readonly ICryptoProvider _cryptoProvider;

      public UserEmailVerificationService(DataContext dataContext, ICryptoProvider cryptoProvider)
      {
         _dataContext = dataContext;
         _cryptoProvider = cryptoProvider;
      }

      public Task<Maybe<UserEmailAddressVerificationParameters>> GenerateVerificationParametersAsync(Guid userId)
      {
         return UserEmailVerificationQueries.GenerateVerificationParametersAsync(_dataContext, _cryptoProvider, userId);
      }

      public async Task SaveVerificationParametersAsync(UserEmailAddressVerificationParameters parameters)
      {
         UserEmailVerificationEntity newEntity = new UserEmailVerificationEntity(parameters.UserId, parameters.VerificationCode, parameters.VerificationKey, DateTime.UtcNow);
         _dataContext.UserEmailVerifications.Add(newEntity);
         await _dataContext.SaveChangesAsync();
      }

      public async Task<bool> VerifyUserEmailAddressAsync(VerifyEmailAddressRequest request)
      {
         Guid verificationCode;
         try
         {
            verificationCode = UrlSafeEncoder.DecodeGuidFromUrlSafe(request.Code);
         }
         catch (Exception)
         {
            return false;
         }

         var verificationEntity = await _dataContext.UserEmailVerifications
            .FirstOrDefaultAsync(x => x.Code == verificationCode);

         if (verificationEntity is null)
         {
            return false;
         }

         byte[] signature = UrlSafeEncoder.DecodeBytesFromUrlSafe(request.Signature);
         if (!_cryptoProvider.DigitalSignature.VerifySignature(verificationEntity.VerificationKey, verificationCode.ToByteArray(), signature))
         {
            return false;
         }

         var user = await _dataContext.Users
            .FirstOrDefaultAsync(x => x.Id == verificationEntity.Owner);

         if (user is not null)
         {
            user.EmailVerified = true;
         }

         _dataContext.UserEmailVerifications.Remove(verificationEntity);
         await _dataContext.SaveChangesAsync();

         return true;
      }
   }
}
