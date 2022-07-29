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
using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Enums;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Text;

namespace Crypter.CryptoLib.Services
{
   public interface IPBKDFService
   {
      byte[] DeriveUserCredentialKey(Username username, Password password, int iterations);
      AuthenticationPassword DeriveUserAuthenticationPassword(Username username, Password password, int iterations);
   }

   /// <summary>
   /// https://stackoverflow.com/a/34990110
   /// </summary>
   public class PBKDFService
   {
      private readonly IDigest _digest;

      public const int CredentialKeySize = 256;
      public const int AuthenticationPasswordSize = 512;

      public PBKDFService()
      {
         _digest = new Sha512Digest();
      }

      public byte[] DeriveUserCredentialKey(Username username, Password password, int iterations)
      {
         return DeriveHashFromCredentials(username, password, iterations, CredentialKeySize);
      }

      public AuthenticationPassword DeriveUserAuthenticationPassword(Username username, Password password, int iterations)
      {
         byte[] hash = DeriveHashFromCredentials(username, password, iterations, AuthenticationPasswordSize);
         return AuthenticationPassword.From(Convert.ToBase64String(hash));
      }

      public byte[] DeriveHashFromCredentials(Username username, Password password, int iterations, int keySize)
      {
         char[] passwordChars = password.Value.ToCharArray();
         byte[] pkcs5CompliantPassword = PbeParametersGenerator.Pkcs5PasswordToBytes(passwordChars);

         SHA digestor = new SHA(SHAFunction.SHA512);
         digestor.BlockUpdate(Encoding.UTF8.GetBytes(username.Value.ToLower()));
         byte[] usernameSalt = digestor.GetDigest();

         Pkcs5S2ParametersGenerator generator = new Pkcs5S2ParametersGenerator(_digest);
         generator.Init(pkcs5CompliantPassword, usernameSalt, iterations);
         var key = (KeyParameter)generator.GenerateDerivedMacParameters(keySize);
         return key.GetKey();
      }
   }
}
