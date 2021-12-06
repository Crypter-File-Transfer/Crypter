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

using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Web.Models;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IAuthenticationService
   {
      Task<bool> Login(string username, string plaintextPassword, string digestedPassword);
      Task Logout();
   }

   public class AuthenticationService : IAuthenticationService
   {
      private readonly NavigationManager NavigationManager;
      private readonly IUserApiService UserApiService;
      private readonly IUserKeysService UserKeysService;
      private readonly ILocalStorageService LocalStorage;

      public AuthenticationService(
          NavigationManager navigationManager,
          IUserApiService userApiService,
          IUserKeysService userKeysService,
          ILocalStorageService localStorage
      )
      {
         NavigationManager = navigationManager;
         UserApiService = userApiService;
         UserKeysService = userKeysService;
         LocalStorage = localStorage;
      }

      public async Task<bool> Login(string username, string plaintextPassword, string digestedPassword)
      {
         var loginRequest = new AuthenticateUserRequest(username, digestedPassword);
         var (httpStatus, authResponse) = await UserApiService.AuthenticateUserAsync(loginRequest);
         if (httpStatus != HttpStatusCode.OK)
         {
            return false;
         }

         var userSession = new UserSession(authResponse.Id, username, authResponse.Token);
         await LocalStorage.SetItem(StoredObjectType.UserSession, userSession, StorageLocation.InMemory);

         await InitializeUserKeys(authResponse, username, plaintextPassword);

         return true;
      }

      public async Task Logout()
      {
         await LocalStorage.Dispose();
         NavigationManager.NavigateTo("/");
      }

      private async Task InitializeUserKeys(UserAuthenticateResponse response, string username, string password)
      {
         if (string.IsNullOrEmpty(response.EncryptedX25519PrivateKey))
         {
            await HandleMissingX25519Keys(response.Id, username, password);
         }
         else
         {
            var decodedX25519Key = Convert.FromBase64String(response.EncryptedX25519PrivateKey);
            await LocalStorage.SetItem(StoredObjectType.EncryptedX25519PrivateKey, decodedX25519Key, StorageLocation.InMemory);

            var decodedIV = Convert.FromBase64String(response.X25519IV);
            var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, decodedX25519Key, decodedIV);
            await LocalStorage.SetItem(StoredObjectType.PlaintextX25519PrivateKey, decryptedKey, StorageLocation.InMemory);
         }

         if (string.IsNullOrEmpty(response.EncryptedEd25519PrivateKey))
         {
            await HandleMissingEd25519Keys(response.Id, username, password);
         }
         else
         {
            var decodedEd25519Key = Convert.FromBase64String(response.EncryptedEd25519PrivateKey);
            await LocalStorage.SetItem(StoredObjectType.EncryptedEd25519PrivateKey, decodedEd25519Key, StorageLocation.InMemory);

            var decodedIV = Convert.FromBase64String(response.Ed25519IV);
            var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, decodedEd25519Key, decodedIV);
            await LocalStorage.SetItem(StoredObjectType.PlaintextEd25519PrivateKey, decryptedKey, StorageLocation.InMemory);
         }
      }

      /// <summary>
      /// Generate and upload a new X25519 key pair.
      /// </summary>
      /// <param name="userId"></param>
      /// <param name="username"></param>
      /// <param name="password"></param>
      private async Task HandleMissingX25519Keys(Guid userId, string username, string password)
      {
         var (encryptedPrivateKey, publicKey, iv) = UserKeysService.GenerateNewX25519KeyPair(username, password);

         var encodedPublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
         var encodedEncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var encodedIV = Convert.ToBase64String(iv);

         var request = new UpdateKeysRequest(encodedEncryptedPrivateKey, encodedPublicKey, encodedIV);
         await UserApiService.InsertUserX25519KeysAsync(request);
         await LocalStorage.SetItem(StoredObjectType.EncryptedX25519PrivateKey, encryptedPrivateKey, StorageLocation.InMemory);

         var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, encryptedPrivateKey, iv);
         await LocalStorage.SetItem(StoredObjectType.PlaintextX25519PrivateKey, decryptedKey, StorageLocation.InMemory);
      }

      /// <summary>
      /// Generate and upload a new Ed25519 key pair.
      /// </summary>
      /// <param name="userId"></param>
      /// <param name="username"></param>
      /// <param name="password"></param>
      private async Task HandleMissingEd25519Keys(Guid userId, string username, string password)
      {
         var (encryptedPrivateKey, publicKey, iv) = UserKeysService.GenerateNewEd25519KeyPair(username, password);

         var encodedPublicKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(publicKey));
         var encodedEncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);
         var encodedIV = Convert.ToBase64String(iv);

         var request = new UpdateKeysRequest(encodedEncryptedPrivateKey, encodedPublicKey, encodedIV);
         await UserApiService.InsertUserEd25519KeysAsync(request);
         await LocalStorage.SetItem(StoredObjectType.EncryptedEd25519PrivateKey, encryptedPrivateKey, StorageLocation.InMemory);

         var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, encryptedPrivateKey, iv);
         await LocalStorage.SetItem(StoredObjectType.PlaintextEd25519PrivateKey, decryptedKey, StorageLocation.InMemory);
      }
   }
}
