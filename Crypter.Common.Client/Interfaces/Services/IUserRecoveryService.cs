/*
 * Copyright (C) 2024 Crypter File Transfer
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

using System.Threading.Tasks;
using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.AccountRecovery.SubmitRecovery;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Primitives;
using EasyMonads;

namespace Crypter.Common.Client.Interfaces.Services;

public interface IUserRecoveryService
{
    /// <summary>
    /// Derive a recovery key from the provided parameters.
    /// </summary>
    /// <param name="masterKey"></param>
    /// <param name="username"></param>
    /// <param name="password">A valid, plaintext password.</param>
    /// <returns></returns>
    Task<Maybe<RecoveryKey>> DeriveRecoveryKeyAsync(byte[] masterKey, Username username, Password password);

    /// <summary>
    /// Derive a recovery key from the provided parameters.
    /// </summary>
    /// <param name="masterKey"></param>
    /// <param name="versionedPassword">A hashed password.</param>
    /// <returns></returns>
    Task<Maybe<RecoveryKey>> DeriveRecoveryKeyAsync(byte[] masterKey, VersionedPassword versionedPassword);

    /// <summary>
    /// Request an account recovery email.
    /// The email, if received, will contain a temporary link to proceed with account recovery.
    /// Account recovery cannot occur without the link.
    /// </summary>
    /// <param name="emailAddress"></param>
    /// <returns></returns>
    Task RequestRecoveryEmailAsync(EmailAddress emailAddress);

    /// <summary>
    /// Proceed with account recovery by submitting necessary recovery information, including secrets from the
    /// account recovery link, the current username, a new password, and an optional recovery key.
    ///
    /// The user's pre-existing master key will be preserved and re-encrypted if the correct recovery key is
    /// provided. Meaning, all encrypted data saved on the server can be recovered.
    ///
    /// If a recovery key is not provided, the user must generate a new master key and will necessarily lose access
    /// to all encrypted data saved on the server.
    /// </summary>
    /// <param name="recoveryCode"></param>
    /// <param name="recoverySignature"></param>
    /// <param name="username"></param>
    /// <param name="newPassword"></param>
    /// <param name="recoveryKey"></param>
    /// <returns></returns>
    Task<Either<SubmitAccountRecoveryError, Maybe<RecoveryKey>>> SubmitRecoveryRequestAsync(string recoveryCode,
        string recoverySignature, Username username, Password newPassword, Maybe<RecoveryKey> recoveryKey);
}
