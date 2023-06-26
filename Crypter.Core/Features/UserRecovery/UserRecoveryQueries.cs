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

using System;
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Primitives;
using Crypter.Core.Models;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.DigitalSignature;
using EasyMonads;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Features.UserRecovery
{
   internal static class UserRecoveryQueries
   {
      internal static async Task<Maybe<UserRecoveryParameters>> GenerateRecoveryParametersAsync(DataContext dataContext, ICryptoProvider cryptoProvider, string emailAddress)
      {
         var userData = await dataContext.Users
            .Where(x => x.EmailAddress == emailAddress)
            .Where(x => x.EmailVerified)
            .Select(x => new { x.Id, x.Username, x.EmailAddress })
            .FirstOrDefaultAsync();

         if (userData is null
            || !Username.TryFrom(userData.Username, out var username)
            || !EmailAddress.TryFrom(userData.EmailAddress, out var validEmailAddress))
         {
            return Maybe<UserRecoveryParameters>.None;
         }

         Guid recoveryCode = Guid.NewGuid();
         Ed25519KeyPair keys = cryptoProvider.DigitalSignature.GenerateKeyPair();
         byte[] signature = GenerateRecoverySignature(cryptoProvider, keys.PrivateKey, recoveryCode, username);
         return new UserRecoveryParameters(userData.Id, username, validEmailAddress, recoveryCode, signature, keys.PublicKey);
      }

      internal static byte[] GenerateRecoverySignature(ICryptoProvider cryptoProvider, ReadOnlySpan<byte> privateKey, Guid recoveryCode, Username username)
      {
         byte[] data = Common.CombineRecoveryCodeWithUsername(recoveryCode, username);
         return cryptoProvider.DigitalSignature.GenerateSignature(privateKey, data);
      }
   }
}
