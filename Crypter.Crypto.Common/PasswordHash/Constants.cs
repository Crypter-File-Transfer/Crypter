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

namespace Crypter.Crypto.Common.PasswordHash
{
   public static class Constants
   {
      // Algorithms
      public const int DEFAULT = ALG_ARGON2ID13;
      public const int ALG_ARGON2I13 = 1;
      public const int ALG_ARGON2ID13 = 2;

      // Output constraints
      public const uint BYTES_MIN = 16U;
      public const uint BYTES_MAX = 4294967295U;

      // Password constraints
      public const uint PASSWORD_MIN = 0U;
      public const uint PASSWORD_MAX = 4294967295U;

      // Salt constraints
      public const uint SALT_BYTES = 16U;

      // MemLimits
      public const int MEMLIMIT_MIN = 8192;
      public const int MEMLIMIT_INTERACTIVE = 67108864;
      public const int MEMLIMIT_MODERATE = 268435456;
      public const int MEMLIMIT_SENSITIVE = 1073741824;
      public const int MEMLIMIT_MAX = 2147483647;

      // OpsLimits
      public const uint OPSLIMIT_MIN = 1U;
      public const uint OPSLIMIT_INTERACTIVE = 2U;
      public const uint OPSLIMIT_MODERATE = 3U;
      public const uint OPSLIMIT_SENSITIVE = 4U;
      public const uint OPSLIMIT_MAX = 4294967295U;
   }
}
