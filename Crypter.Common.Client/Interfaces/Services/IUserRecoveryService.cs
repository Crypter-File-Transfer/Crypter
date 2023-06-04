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

using Crypter.Common.Client.Models;
using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Contracts.Features.UserRecovery.SubmitRecovery;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Interfaces.Services
{
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
      /// Derivce a recovery key from the provided parameters.
      /// </summary>
      /// <param name="masterKey"></param>
      /// <param name="username"></param>
      /// <param name="versionedPassword">A hashed password.</param>
      /// <returns></returns>
      Task<Maybe<RecoveryKey>> DeriveRecoveryKeyAsync(byte[] masterKey, Username username, VersionedPassword versionedPassword);

      Task RequestRecoveryEmailAsync(EmailAddress emailAddress);
      Task<Either<SubmitRecoveryError, Maybe<RecoveryKey>>> SubmitRecoveryRequestAsync(string recoveryCode, string recoverySignature, Username username, Password newPassword, Maybe<RecoveryKey> recoveryProof);
   }
}
