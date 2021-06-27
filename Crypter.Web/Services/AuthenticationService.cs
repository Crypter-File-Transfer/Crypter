using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Crypter.Contracts.Requests;
using System;
using System.Text;
using System.Net;

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
            if (string.IsNullOrEmpty(User.PrivateKey))
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

         if (string.IsNullOrEmpty(authResponse.EncryptedPrivateKey))
         {
            User.PrivateKey = null;
         }
         else
         {
            var decryptionKey = CryptoLib.Common.CreateSymmetricKeyFromUserDetails(username, plaintextPassword, authResponse.Id.ToString());
            byte[] decodedPrivateKey = Convert.FromBase64String(authResponse.EncryptedPrivateKey);
            byte[] decryptedPrivateKey = CryptoLib.Common.UndoSymmetricEncryption(decodedPrivateKey, decryptionKey);
            User.PrivateKey = Encoding.UTF8.GetString(decryptedPrivateKey);
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
