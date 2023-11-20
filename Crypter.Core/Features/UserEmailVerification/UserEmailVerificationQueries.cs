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

namespace Crypter.Core.Features.UserEmailVerification;

internal static class UserEmailVerificationQueries
{
    internal static async Task<Maybe<UserEmailAddressVerificationParameters>> GenerateVerificationParametersAsync(
        DataContext dataContext, ICryptoProvider cryptoProvider, Guid userId)
    {
        var user = await dataContext.Users
            .Where(x => x.Id == userId)
            .Where(x => !string.IsNullOrEmpty(x.EmailAddress))
            .Where(x => !x.EmailVerified)
            .Where(x => x.EmailVerification == null)
            .Select(x => new { x.Id, x.EmailAddress })
            .FirstOrDefaultAsync();

        if (user is null || !EmailAddress.TryFrom(user.EmailAddress, out EmailAddress validEmailAddress))
        {
            return Maybe<UserEmailAddressVerificationParameters>.None;
        }

        Guid verificationCode = Guid.NewGuid();
        Ed25519KeyPair keys = cryptoProvider.DigitalSignature.GenerateKeyPair();

        byte[] signature =
            cryptoProvider.DigitalSignature.GenerateSignature(keys.PrivateKey, verificationCode.ToByteArray());

        return new UserEmailAddressVerificationParameters(userId, validEmailAddress, verificationCode, signature,
            keys.PublicKey);
    }
}
