using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Crypter.Contracts.Requests;
using System;
using System.Text;
using System.Net;
using Crypter.Web.Services.API;

namespace Crypter.Web.Services
{

   public interface IAuthenticationService
   {
      User User { get; }
      Task Initialize();
      Task<bool> Login(string username, string plaintextPassword, string digestedPassword);
      Task Refresh();
      Task Logout();
   }

   public class AuthenticationService : IAuthenticationService
   {
      private readonly NavigationManager NavigationManager;
      private readonly ILocalStorageService LocalStorageService;
      private readonly IUserService UserService;

      public User User { get; private set; }

      public AuthenticationService(
          NavigationManager navigationManager,
          ILocalStorageService localStorageService,
          IUserService userService
      )
      {
         NavigationManager = navigationManager;
         LocalStorageService = localStorageService;
         UserService = userService;
      }

      public async Task Initialize()
      {
         User = await LocalStorageService.GetItem<User>("user");
         if (User is not null)
         {
            if (string.IsNullOrEmpty(User.X25519PrivateKey) || string.IsNullOrEmpty(User.Ed25519PrivateKey))
            {
               await Logout();
            }
            else
            {
               await Refresh();
            }

         }
      }

      public async Task<bool> Login(string username, string plaintextPassword, string digestedPassword)
      {
         var loginRequest = new AuthenticateUserRequest(username, digestedPassword);
         var (httpStatus, authResponse) = await UserService.AuthenticateUserAsync(loginRequest);
         if (httpStatus != HttpStatusCode.OK)
         {
            return false;
         }

         User = new User(authResponse.Id, authResponse.Token);

         if (string.IsNullOrEmpty(authResponse.EncryptedX25519PrivateKey))
         {
            User.X25519PrivateKey = null;
         }
         else
         {
            (var key, var iv) = CryptoLib.UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(username, plaintextPassword, authResponse.Id);
            var decodedX25519PrivateKey = Convert.FromBase64String(authResponse.EncryptedX25519PrivateKey);
            var decodedEd25519PrivateKey = Convert.FromBase64String(authResponse.EncryptedEd25519PrivateKey);

            var decrypter = new CryptoLib.Crypto.AES();
            decrypter.Initialize(key, iv, false);
            var decryptedX25519PrivateKey = decrypter.ProcessFinal(decodedX25519PrivateKey);

            decrypter.Initialize(key, iv, false);
            var decryptedEd25519PrivateKey = decrypter.ProcessFinal(decodedEd25519PrivateKey);

            User.X25519PrivateKey = Encoding.UTF8.GetString(decryptedX25519PrivateKey);
            User.Ed25519PrivateKey = Encoding.UTF8.GetString(decryptedEd25519PrivateKey);
         }

         await LocalStorageService.SetItem("user", User);
         return true;
      }

      public async Task Refresh()
      {
         var (httpStatus, refreshResponse) = await UserService.RefreshAuthenticationAsync();
         if (httpStatus == HttpStatusCode.Unauthorized)
         {
            await Logout();
         }
         else
         {
            User.Token = refreshResponse.Token;
            await LocalStorageService.SetItem("user", User);
         }
      }

      public async Task Logout()
      {
         User = null;
         await LocalStorageService.RemoveItem("user");
         NavigationManager.NavigateTo("/");
      }
   }
}
