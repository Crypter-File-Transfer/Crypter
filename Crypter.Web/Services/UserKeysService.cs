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

using System;
using System.Text;

namespace Crypter.Web.Services
{
   public interface IUserKeysService
   {
      /// <summary>
      /// Generate a new X25519 key pair.
      /// </summary>
      /// <param name="userId"></param>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <returns>Encrypted private key and plaintext public key in PEM format</returns>
      (byte[] encryptedPrivateKey, string publicKey) GenerateNewX25519KeyPair(Guid userId, string username, string password);

      /// <summary>
      /// Generate a new Ed25519 key pair.
      /// </summary>
      /// <param name="userId"></param>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <returns>Encrypted private key and plaintext public key in PEM format</returns>
      public (byte[] encryptedPrivateKey, string publicKey) GenerateNewEd25519KeyPair(Guid userId, string username, string password);

      /// <summary>
      /// Decrypts and returns the provided Curve25519 private key
      /// </summary>
      /// <param name="username"></param>
      /// <param name="password"></param>
      /// <param name="userId"></param>
      /// <returns>Private key in PEM format</returns>
      public string DecryptPrivateKey(string username, string password, Guid userId, byte[] privateKey);
   }

   public class UserKeysService : IUserKeysService
   {
      public (byte[] encryptedPrivateKey, string publicKey) GenerateNewX25519KeyPair(Guid userId, string username, string password)
      {
         var keyPair = CryptoLib.Crypto.ECDH.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);
         var encryptedPrivateKey = EncryptPrivateKey(privateKey, userId, username, password);

         return (encryptedPrivateKey, publicKey);
      }

      public (byte[] encryptedPrivateKey, string publicKey) GenerateNewEd25519KeyPair(Guid userId, string username, string password)
      {
         var keyPair = CryptoLib.Crypto.ECDSA.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);
         var encryptedPrivateKey = EncryptPrivateKey(privateKey, userId, username, password);

         return (encryptedPrivateKey, publicKey);
      }

      public string DecryptPrivateKey(string username, string password, Guid userId, byte[] privateKey)
      {
         (var key, var iv) = CryptoLib.UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username, password, userId);
         var decrypter = new CryptoLib.Crypto.AES();
         decrypter.Initialize(key, iv, false);
         var decrypted = decrypter.ProcessFinal(privateKey);
         return Encoding.UTF8.GetString(decrypted);
      }

      private static byte[] EncryptPrivateKey(string privatePemKey, Guid userId, string username, string password)
      {
         (var key, var iv) = CryptoLib.UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username.ToLower(), password, userId);
         var encrypter = new CryptoLib.Crypto.AES();
         encrypter.Initialize(key, iv, true);
         return encrypter.ProcessFinal(Encoding.UTF8.GetBytes(privatePemKey));
      }
   }
}
