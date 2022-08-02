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
using Crypter.Common.Models;
using Crypter.Common.Primitives;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crypter.ClientServices.Services
{
   public static class ClientPBKDFServiceExtensions
   {
      public static void ClientPBKDFServiceService(this IServiceCollection services, Action<List<PasswordVersion>> settings)
      {
         if (settings is null)
         {
            throw new ArgumentNullException(nameof(settings));
         }

         services.Configure(settings);
         services.TryAddSingleton<IClientPBKDFService, ClientPBKDFService>();
      }
   }

   /// <summary>
   /// https://stackoverflow.com/a/34990110
   /// </summary>
   public class ClientPBKDFService : IClientPBKDFService
   {
      private readonly IReadOnlyDictionary<int, PasswordVersion> _passwordVersions;
      private readonly IDigest _digest;

      public const int CredentialKeySize = 256;
      public const int AuthenticationPasswordSize = 512;

      public int CurrentPasswordVersion { get; init; }

      public ClientPBKDFService(IOptions<List<PasswordVersion>> settings)
      {
         _passwordVersions = settings.Value.ToDictionary(x => x.Version);
         _digest = new Sha512Digest();

         CurrentPasswordVersion = _passwordVersions.Keys.Max();
      }

      public byte[] DeriveUserCredentialKey(Username username, Password password, int passwordVersion)
      {
         return passwordVersion switch
         {
            0 =>
#pragma warning disable CS0618
               UserFunctions.DeriveSymmetricKeyFromUserCredentials(username, password),
#pragma warning restore CS0618
            1 => DeriveHashFromCredentials(username, password, _passwordVersions[passwordVersion].Iterations, CredentialKeySize),
            _ => throw new NotImplementedException()
         };
      }

      public AuthenticationPassword DeriveUserAuthenticationPassword(Username username, Password password, int passwordVersion)
      {
         switch (passwordVersion)
         {
            case 0:
#pragma warning disable CS0618
               return UserFunctions.DeriveAuthenticationPasswordFromUserCredentials(username, password);
#pragma warning restore CS0618
            case 1:
               byte[] hash = DeriveHashFromCredentials(username, password, _passwordVersions[passwordVersion].Iterations, AuthenticationPasswordSize);
               return AuthenticationPassword.From(Convert.ToBase64String(hash));
            default:
               throw new NotImplementedException();
         }
      }

      public byte[] DeriveHashFromCredentials(Username username, Password password, int iterations, int keySize)
      {
         char[] passwordChars = password.Value.ToCharArray();
         byte[] pkcs5CompliantPassword = PbeParametersGenerator.Pkcs5PasswordToBytes(passwordChars);
         byte[] usernameSalt = CryptoHash.Sha512(username.Value.ToLower());

         Pkcs5S2ParametersGenerator generator = new Pkcs5S2ParametersGenerator(_digest);
         generator.Init(pkcs5CompliantPassword, usernameSalt, iterations);
         var key = (KeyParameter)generator.GenerateDerivedMacParameters(keySize);
         return key.GetKey();
      }
   }
}
