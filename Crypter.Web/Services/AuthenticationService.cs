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

            var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, response.Id, decodedX25519Key);
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

            var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, response.Id, decodedEd25519Key);
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
         var (encryptedPrivateKey, publicKey) = UserKeysService.GenerateNewX25519KeyPair(userId, username, password);

         var encodedPublicKey = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(publicKey));

         var encodedEncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);

         var request = new UpdateKeysRequest(encodedEncryptedPrivateKey, encodedPublicKey);
         await UserApiService.InsertUserX25519KeysAsync(request);
         await LocalStorage.SetItem(StoredObjectType.EncryptedX25519PrivateKey, encryptedPrivateKey, StorageLocation.InMemory);

         var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, userId, encryptedPrivateKey);
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
         var (encryptedPrivateKey, publicKey) = UserKeysService.GenerateNewEd25519KeyPair(userId, username, password);

         var encodedPublicKey = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(publicKey));

         var encodedEncryptedPrivateKey = Convert.ToBase64String(encryptedPrivateKey);

         var request = new UpdateKeysRequest(encodedEncryptedPrivateKey, encodedPublicKey);
         await UserApiService.InsertUserEd25519KeysAsync(request);
         await LocalStorage.SetItem(StoredObjectType.EncryptedEd25519PrivateKey, encryptedPrivateKey, StorageLocation.InMemory);

         var decryptedKey = UserKeysService.DecryptPrivateKey(username, password, userId, encryptedPrivateKey);
         await LocalStorage.SetItem(StoredObjectType.PlaintextEd25519PrivateKey, decryptedKey, StorageLocation.InMemory);
      }
   }
}
