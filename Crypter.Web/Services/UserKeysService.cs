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

namespace Crypter.Web.Services
{
   public interface IUserKeysService
   {
      /// <summary>
      /// Generate a new X25519 key pair.
      /// </summary>
      /// <returns>Private and public keys in PEM format</returns>
      (string privateKey, string publicKey) NewX25519KeyPair();

      /// <summary>
      /// Generate a new Ed25519 key pair.
      /// </summary>
      /// <returns>Private and public keys in PEM format</returns>
      (string privateKey, string publicKey) NewEd25519KeyPair();

      /// <summary>
      /// Get the user's secret, symmetric key.
      /// </summary>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <returns>Array of 32 bytes.</returns>
      byte[] GetUserSymmetricKey(string username, string password);
   }

   public class UserKeysService : IUserKeysService
   {
      public (string privateKey, string publicKey) NewX25519KeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDH.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);

         return (privateKey, publicKey);
      }

      public (string privateKey, string publicKey) NewEd25519KeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDSA.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);

         return (privateKey, publicKey);
      }

      public byte[] GetUserSymmetricKey(string username, string password)
      {
         return CryptoLib.UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, password);
      }
   }
}
