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

using Crypter.ClientServices.Interfaces;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication;
using Crypter.CryptoLib.Sodium;
using System;
using System.Collections.Generic;
using System.Text;

namespace Crypter.ClientServices.Services
{
   public class UserPasswordService : IUserPasswordService
   {
      private const int _currentPasswordVersion = 1;
      private const int _credentialKeySize = 32;
      private const int _authenticationPasswordSize = 64;

      public int CurrentPasswordVersion
      { get { return _currentPasswordVersion; } }

      public int CredentialKeySize
      { get { return _credentialKeySize; } }

      public int AuthenticationPasswordSize
      { get { return _authenticationPasswordSize; } }

      public byte[] DeriveUserCredentialKey(Username username, Password password, int passwordVersion)
      {
#pragma warning disable CS0618
         return passwordVersion switch
         {
            0 => DeriveSha256CredentialKey(username, password),
            1 => DeriveArgonKey(username, password, _credentialKeySize),
            _ => throw new NotImplementedException()
         };
#pragma warning restore CS0618
      }

      public VersionedPassword DeriveUserAuthenticationPassword(Username username, Password password, int passwordVersion)
      {
#pragma warning disable CS0618
         return passwordVersion switch
         {
            0 => DeriveSha512AuthenticationPassword(username, password),
            1 => DeriveArgonAuthenticationPassword(username, password),
            _ => throw new NotImplementedException()
         };
#pragma warning restore CS0618
      }

      [Obsolete("Use DeriveArgonKey instead")]
      public static byte[] DeriveSha256CredentialKey(Username username, Password password)
      {
         List<byte[]> seedData = new List<byte[]>
         {
            Encoding.UTF8.GetBytes(username.Value.ToLower()),
            Encoding.UTF8.GetBytes(password.Value)
         };

         return CryptoHash.Sha256(seedData);
      }

      [Obsolete("Use DeriveArgonAuthenticationPassword instead")]
      public static VersionedPassword DeriveSha512AuthenticationPassword(Username username, Password password)
      {
         List<byte[]> seedData = new List<byte[]>
         {
            Encoding.UTF8.GetBytes(password.Value),
            Encoding.UTF8.GetBytes(username.Value.ToLower())
         };
         byte[] hashedPassword = CryptoHash.Sha512(seedData);
         string encodedPassword = Convert.ToBase64String(hashedPassword);
         AuthenticationPassword authPassword = AuthenticationPassword.From(encodedPassword);
         return new VersionedPassword(authPassword, 0);
      }

      public static VersionedPassword DeriveArgonAuthenticationPassword(Username username, Password password)
      {
         byte[] hashedPassword = DeriveArgonKey(username, password, _authenticationPasswordSize);
         string encodedPassword = Convert.ToBase64String(hashedPassword);
         AuthenticationPassword authPassword = AuthenticationPassword.From(encodedPassword);
         return new VersionedPassword(authPassword, 1);
      }

      public static byte[] DeriveArgonKey(Username username, Password password, int keyLength)
      {
         byte[] salt = PasswordHash.ArgonDeriveSalt(username.Value.ToLower());
         return PasswordHash.ArgonHash(password.Value, salt, keyLength, PasswordHash.Strength.Sensitive);
      }
   }
}
