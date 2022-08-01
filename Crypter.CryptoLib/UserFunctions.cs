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

using Crypter.Common.Primitives;
using Crypter.CryptoLib.Enums;
using System;
using System.Text;

namespace Crypter.CryptoLib
{
   public static class UserFunctions
   {
      /// <summary>
      /// Digest the user's credentials.
      /// </summary>
      /// <param name="username">Username will be lowercased within the method.</param>
      /// <param name="password"></param>
      /// <returns>AuthenticationPassword</returns>
      /// <remarks>
      /// The result of this method gets used as the user's password during authentication requests.
      /// The reason for doing this is to keep the user's real password a secret from even our own API.
      /// </remarks>
      [Obsolete("Use PBKDFService")]
      public static AuthenticationPassword DeriveAuthenticationPasswordFromUserCredentials(Username username, Password password)
      {
         var digestor = new Crypto.SHA(SHAFunction.SHA512);
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(password.Value));
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(username.Value.ToLower()));
         byte[] digest = digestor.GetDigest();

         string base64 = Convert.ToBase64String(digest);
         return AuthenticationPassword.From(base64);
      }

      /// <summary>
      /// Use the user's credentials to create a symmetric key.
      /// </summary>
      /// <param name="username">Username will be lowercased within the method.</param>
      /// <param name="password"></param>
      /// <returns>Array of 32 bytes.</returns>
      [Obsolete("Use PBKDFService")]
      public static byte[] DeriveSymmetricKeyFromUserCredentials(Username username, Password password)
      {
         var keyDigestor = new Crypto.SHA(SHAFunction.SHA256);
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(username.Value.ToLower()));
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(password.Value));
         return keyDigestor.GetDigest();
      }
   }
}
