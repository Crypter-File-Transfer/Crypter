﻿/*
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

using Crypter.Common.Primitives;
using Crypter.Core.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace Crypter.Core.Services
{
   public interface IPasswordHashService
   {
      SecurePasswordHashOutput MakeSecurePasswordHash(AuthenticationPassword password, int iterations);
      bool VerifySecurePasswordHash(AuthenticationPassword password, byte[] existingHash, byte[] existingSalt, int iterations);
   }

   public class PasswordHashService : IPasswordHashService
   {
      private static readonly KeyDerivationPrf KeyDerivationAlgorithm = KeyDerivationPrf.HMACSHA512;
      private static readonly int HashByteLength = 64; // 512 bits
      private static readonly int SaltByteLength = 16; // 128 bits

      public SecurePasswordHashOutput MakeSecurePasswordHash(AuthenticationPassword password, int iterations)
      {
         var salt = RandomNumberGenerator.GetBytes(SaltByteLength);
         var hash = KeyDerivation.Pbkdf2(password.Value, salt, KeyDerivationAlgorithm, iterations, HashByteLength);
         return new SecurePasswordHashOutput { Hash = hash, Salt = salt };
      }

      public bool VerifySecurePasswordHash(AuthenticationPassword password, byte[] existingHash, byte[] existingSalt, int iterations)
      {
         if (existingHash.Length != HashByteLength)
         {
            throw new ArgumentException("Invalid length of password hash (64 bytes expected).");
         }

         if (existingSalt.Length != SaltByteLength)
         {
            throw new ArgumentException("Invalid length of password salt (16 bytes expected).");
         }

         var computedHash = KeyDerivation.Pbkdf2(password.Value, existingSalt, KeyDerivationAlgorithm, iterations, HashByteLength);

         for (int i = 0; i < computedHash.Length; i++)
         {
            if (computedHash[i] != existingHash[i])
            {
               return false;
            }
         }

         return true;
      }
   }
}
