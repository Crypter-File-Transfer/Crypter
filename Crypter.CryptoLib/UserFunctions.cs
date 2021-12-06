/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.CryptoLib.Enums;
using System.Text;

namespace Crypter.CryptoLib
{
   public static class UserFunctions
   {
      /// <summary>
      /// Digest a user's login information.
      /// </summary>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <returns>Array of 64 bytes.</returns>
      /// <remarks>
      /// The result of this method is used as the user's password during authentication requests.
      /// </remarks>
      public static byte[] DigestUserCredentials(string username, string password)
      {
         var digestor = new Crypto.SHA(SHAFunction.SHA512);
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(password));
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(username.ToLower()));
         return digestor.GetDigest();
      }

      /// <summary>
      /// Derive a symmetric encryption key from the user's login information
      /// </summary>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <returns>Array of 32 bytes.</returns>
      public static byte[] DeriveSymmetricCryptoParamsFromUserDetails(string username, string password)
      {
         var keyDigestor = new Crypto.SHA(SHAFunction.SHA256);
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(username.ToLower()));
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(password));
         return keyDigestor.GetDigest();
      }
   }
}
