using Crypter.Contracts.Requests;
using Crypter.Web.Helpers;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class LoginComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected ITransferService UploadService { get; set; }

      [Inject]
      protected IUserService UserService { get; set; }

      [Inject]
      protected AppSettings AppSettings { get; set; }

      protected Login loginInfo = new();

      protected bool LoginError = false;
      protected string LoginErrorText = "";

      protected override async Task OnInitializedAsync()
      {
         if (AuthenticationService.User is not null)
         {
            NavigationManager.NavigateTo("/user/home");
         }
         await base.OnInitializedAsync();
      }

      protected async Task OnLoginClicked(int recurseCount = 0)
      {
         if (recurseCount > 1)
         {
            LoginErrorText = "Something went wrong";
            return;
         }

         byte[] digestedPassword = CryptoLib.UserFunctions.DigestUserCredentials(loginInfo.Username, loginInfo.Password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var authSuccess = await AuthenticationService.Login(loginInfo.Username, loginInfo.Password, digestedPasswordBase64);
         if (authSuccess)
         {
            if (!string.IsNullOrEmpty(AuthenticationService.User.X25519PrivateKey)
               && !string.IsNullOrEmpty(AuthenticationService.User.Ed25519PrivateKey))
            {
               OnLoginCompleted();
               return;
            }

            if (string.IsNullOrEmpty(AuthenticationService.User.X25519PrivateKey))
            {
               await GenerateAndUploadX25519Keys();
            }
            
            if (string.IsNullOrEmpty(AuthenticationService.User.Ed25519PrivateKey))
            {
               await GenerateAndUploadEd25519Keys();
            }

            await OnLoginClicked(recurseCount++);
         }
         else
         {
            LoginError = true;
            LoginErrorText = "Incorrect username or password";
         }
      }

      protected void OnLoginCompleted()
      {
         var returnUrl = NavigationManager.QueryString("returnUrl") ?? "user/home";
         NavigationManager.NavigateTo(returnUrl);
      }

      protected async Task GenerateAndUploadX25519Keys()
      {
         var (dhPrivateKey, dhPublicKey) = GenerateUserDHKeyPair();
         var encodedPublicKey = Convert.ToBase64String(
             Encoding.UTF8.GetBytes(dhPublicKey));

         var encodedEncryptedPrivateKey = Convert.ToBase64String(
             EncryptPrivateKey(dhPrivateKey, AuthenticationService.User.Id));


         var request = new UpdateKeysRequest(encodedEncryptedPrivateKey, encodedPublicKey);
         await UserService.InsertUserX25519KeysAsync(request);
      }

      protected async Task GenerateAndUploadEd25519Keys()
      {
         var (dhPrivateKey, dhPublicKey) = GenerateUserDSAKeyPair();
         var encodedPublicKey = Convert.ToBase64String(
             Encoding.UTF8.GetBytes(dhPublicKey));

         var encodedEncryptedPrivateKey = Convert.ToBase64String(
             EncryptPrivateKey(dhPrivateKey, AuthenticationService.User.Id));


         var request = new UpdateKeysRequest(encodedEncryptedPrivateKey, encodedPublicKey);
         await UserService.InsertUserEd25519KeysAsync(request);
      }

      protected static (string privateKey, string publicKey) GenerateUserDHKeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDH.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);
         return (privateKey, publicKey);
      }

      protected static (string privateKey, string publicKey) GenerateUserDSAKeyPair()
      {
         var keyPair = CryptoLib.Crypto.ECDSA.GenerateKeys();
         var privateKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Private);
         var publicKey = CryptoLib.KeyConversion.ConvertToPEM(keyPair.Public);
         return (privateKey, publicKey);
      }

      protected byte[] EncryptPrivateKey(string privatePemKey, Guid userId)
      {
         (var key, var iv) = CryptoLib.UserFunctions.DeriveSymmetricCryptoParamsFromUserDetails(loginInfo.Username.ToLower(), loginInfo.Password, userId);
         var encrypter = new CryptoLib.Crypto.AES();
         encrypter.Initialize(key, iv, true);
         return encrypter.ProcessFinal(Encoding.UTF8.GetBytes(privatePemKey));
      }
   }
}
