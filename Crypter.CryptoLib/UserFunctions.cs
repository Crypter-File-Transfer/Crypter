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
using System;
using System.Text;

namespace Crypter.CryptoLib
{
   public static class UserFunctions
   {
      public static byte[] DigestUserCredentials(string username, string password)
      {
         var digestor = new Crypto.SHA(SHAFunction.SHA512);
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(password));
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(username.ToLower()));
         return digestor.GetDigest();
      }

      /// <summary>
      /// Create the symmetric parameters to encrypt/decrypt a user's stored data
      /// </summary>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <param name="userId"></param>
      /// <returns></returns>
      public static (byte[] Key, byte[] IV) DeriveSymmetricCryptoParamsFromUserDetails(string username, string password, Guid userId)
      {
         var keyDigestor = new Crypto.SHA(SHAFunction.SHA256);
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(username.ToLower()));
         keyDigestor.BlockUpdate(Encoding.UTF8.GetBytes(password));
         var key = keyDigestor.GetDigest();

         var ivDigestor = new Crypto.SHA(SHAFunction.SHA256);
         ivDigestor.BlockUpdate(Encoding.UTF8.GetBytes(userId.ToString().ToLower()));
         var iv = ivDigestor.GetDigest()[0..16];
         return (key, iv);
      }
   }
}
