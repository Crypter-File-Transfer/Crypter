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

using System.Text;

namespace Crypter.CryptoLib
{
   /// <summary>
   /// https://github.com/ektrah/libsodium-core/blob/master/src/Sodium.Core/PasswordHash.cs
   /// </summary>
   public static class PasswordHash
   {
      public enum Strength
      {
         Interactive = 0,
         Medium = 1,
         Moderate = 2,
         Sensitive = 3
      }

      public static byte[] ArgonGenerateSalt()
      {
         return Sodium.PasswordHash.ArgonGenerateSalt();
      }

      public static byte[] ArgonDeriveSalt(string fromValue)
      {
         return GenericHash.Hash(fromValue, 16);
      }

      public static byte[] ArgonHash(byte[] password, byte[] salt, long outputLength, Strength strength)
      {
         return Sodium.PasswordHash.ArgonHashBinary(password, salt, (Sodium.PasswordHash.StrengthArgon)strength, outputLength, Sodium.PasswordHash.ArgonAlgorithm.Argon_2ID13);
      }

      public static byte[] ArgonHash(string password, byte[] salt, long outputLength, Strength strength)
      {
         byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
         return ArgonHash(passwordBytes, salt, outputLength, strength);
      }

      public static bool ArgonVerify(byte[] password, byte[] hash)
      {
         return Sodium.PasswordHash.ArgonHashStringVerify(hash, password);
      }
   }
}
