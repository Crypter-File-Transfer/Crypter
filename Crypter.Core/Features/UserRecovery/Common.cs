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
using System.Text;
using Crypter.Common.Primitives;
using Crypter.Crypto.Common;

namespace Crypter.Core.Features.UserRecovery
{
   internal static class Common
   {
      internal static byte[] CombineRecoveryCodeWithUsername(Guid recoveryCode, Username username)
      {
         byte[] recoveryCodeBytes = recoveryCode.ToByteArray();
         byte[] usernameBytes = Encoding.UTF8.GetBytes(username.Value);

         return recoveryCodeBytes.Concat(usernameBytes).ToArray();
      }

      internal static bool VerifyRecoverySignature(ICryptoProvider cryptoProvider, ReadOnlySpan<byte> publicKey, Guid recoveryCode, Username username, ReadOnlySpan<byte> signature)
      {
         byte[] data = CombineRecoveryCodeWithUsername(recoveryCode, username);
         return cryptoProvider.DigitalSignature.VerifySignature(publicKey, data, signature);
      }

      internal static bool RecoveryCodesMatch(ICryptoProvider cryptoProvider, Guid left, Guid right)
      {
         byte[] leftBytes = left.ToByteArray();
         byte[] rightBytes = right.ToByteArray();

         return cryptoProvider.ConstantTime.Equals(leftBytes, rightBytes);
      }
   }
}
