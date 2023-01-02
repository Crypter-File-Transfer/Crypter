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

using Crypter.Common.Monads;
using System;

namespace Crypter.Crypto.Common.PasswordHash
{
   public abstract class AbstractPasswordHash : IPasswordHash
   {
      public uint SaltSize { get => Constants.SALT_BYTES; }

      public Either<Exception, byte[]> GenerateKey(string password, ReadOnlySpan<byte> salt, uint outputLength, OpsLimit opsLimit, MemLimit memLimit)
      {
#pragma warning disable CS8524
         uint opsLimitNum = opsLimit switch
         {
            OpsLimit.Minimum => Constants.OPSLIMIT_MIN,
            OpsLimit.Interactive => Constants.OPSLIMIT_INTERACTIVE,
            OpsLimit.Moderate => Constants.OPSLIMIT_MODERATE,
            OpsLimit.Sensitive => Constants.OPSLIMIT_SENSITIVE,
            OpsLimit.Maximum => Constants.OPSLIMIT_MAX
         };
#pragma warning restore CS8524

#pragma warning disable CS8524
         uint memLimitNum = memLimit switch
         {
            MemLimit.Minimum => Constants.MEMLIMIT_MIN,
            MemLimit.Interactive => Constants.MEMLIMIT_INTERACTIVE,
            MemLimit.Moderate => Constants.MEMLIMIT_MODERATE,
            MemLimit.Sensitive => Constants.MEMLIMIT_SENSITIVE,
            MemLimit.Maximum => Constants.MEMLIMIT_MAX
         };
#pragma warning restore CS8524

         return HashImplementation(password, salt, outputLength, opsLimitNum, memLimitNum);
      }

      protected abstract Either<Exception, byte[]> HashImplementation(string password, ReadOnlySpan<byte> salt, uint outputLength, uint opsLimit, uint memLimit);
   }
}
