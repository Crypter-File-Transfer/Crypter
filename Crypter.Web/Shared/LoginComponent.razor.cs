using Crypter.Contracts.Requests;
using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Enums;
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
      protected IUploadService UploadService { get; set; }

      [Inject]
      protected IUserService UserService { get; set; }

      [Inject]
      protected AppSettings AppSettings { get; set; }

      protected Modal.BasicModal BasicModal { get; set; }
      protected Modal.SpinnerModal SpinnerModal { get; set; }

      protected Login loginInfo = new();

      protected bool LoginError = false;
      protected string LoginErrorText = "";

      protected string BasicModalSubject;
      protected string BasicModalMessage;
      protected string BasicModalPrimaryButtonText;
      protected string BasicModalSecondaryButtonText;
      protected bool BasicModalShowSecondaryButton;
      protected Func<bool, Task> BasicModalClosedCallback;

      protected string SpinnerModalSubject;
      protected string SpinnerModalMessage;
      protected string SpinnerModalPrimaryButtonText;
      protected bool SpinnerModalShowPrimaryButton;
      protected Action<bool> SpinnerModalClosedCallback;

      protected override async Task OnInitializedAsync()
      {
         if (AuthenticationService.User is not null)
         {
            NavigationManager.NavigateTo("/user/home");
         }
         await base.OnInitializedAsync();
      }

      protected async Task OnLoginClicked()
      {
         byte[] digestedPassword = CryptoLib.Common.DigestUsernameAndPasswordForAuthentication(loginInfo.Username, loginInfo.Password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var authSuccess = await AuthenticationService.Login(loginInfo.Username, loginInfo.Password, digestedPasswordBase64);
         if (authSuccess)
         {
            if (string.IsNullOrEmpty(AuthenticationService.User.PrivateKey))
            {
               PromptUserToCreateKeyPair();
            }
            else
            {
               OnLoginCompleted();
            }
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

      protected async Task GenerateAndUploadKeys()
      {
         var (privateKey, publicKey) = await GenerateUserKeyPair();
         var encodedPublicKey = Convert.ToBase64String(
             Encoding.UTF8.GetBytes(publicKey));

         var encodedEncryptedPrivateKey = Convert.ToBase64String(
             EncryptPrivateKey(privateKey));


         var request = new UpdateUserKeysRequest(encodedEncryptedPrivateKey, encodedPublicKey);
         await UserService.UpdateUserKeysAsync(request);
      }

      protected static async Task<(string privateKey, string publicKey)> GenerateUserKeyPair()
      {
         var keyPair = await Task.Run(() => CryptoLib.Common.GenerateAsymmetricKeys(CryptoStrength.Standard));
         var privateKey = keyPair.Private.ConvertToPEM();
         var publicKey = keyPair.Public.ConvertToPEM();
         return (privateKey, publicKey);
      }

      protected byte[] EncryptPrivateKey(string privatePemKey)
      {
         var privateKeyBytes = Encoding.UTF8.GetBytes(privatePemKey);
         var symmetricEncryptionKey = CryptoLib.Common.CreateSymmetricKeyFromUserDetails(loginInfo.Username, loginInfo.Password, AuthenticationService.User.Id.ToString());
         return CryptoLib.Common.DoSymmetricEncryption(privateKeyBytes, symmetricEncryptionKey);
      }

      protected void PromptUserToCreateKeyPair()
      {
         BasicModalSubject = "Generate your keys";
         BasicModalMessage = "We need to generate some cryptographic keys to finish setting up your Crypter account." +
            " This usually takes a while and your browser may stop responding." +
            " Please be patient.";
         BasicModalPrimaryButtonText = "Generate Keys";
         BasicModalSecondaryButtonText = "";
         BasicModalShowSecondaryButton = false;
         BasicModalClosedCallback = ProceedWithKeyCreation;
         BasicModal.Open();
      }

      protected async Task ProceedWithKeyCreation(bool closedInTheAffirmative)
      {
         await ShowSpinnerWhileCreatingKeyPair();
         await GenerateAndUploadKeys();
         await SpinnerModal.CloseAsync();
      }

      protected async Task ShowSpinnerWhileCreatingKeyPair()
      {
         SpinnerModalSubject = "Generating your keys";
         SpinnerModalMessage = "Your keys are being generated." +
            " This usually takes a while and your browser may stop responding." +
            " Please be patient.";
         SpinnerModalPrimaryButtonText = "";
         SpinnerModalShowPrimaryButton = false;
         SpinnerModalClosedCallback = InformUserKeysCreated;
         SpinnerModal.Open();
         StateHasChanged();
         await Task.Delay(500);
      }

      protected void InformUserKeysCreated(bool closedInTheAffirmative)
      {
         BasicModalSubject = "Done";
         BasicModalMessage = "Your keys have been generated. Click 'OK' to continue logging in.";
         BasicModalPrimaryButtonText = "OK";
         BasicModalClosedCallback = ProceedWithLoginAfterKeyCreation;
         StateHasChanged();
         BasicModal.Open();
      }

      protected async Task ProceedWithLoginAfterKeyCreation(bool closedInTheAffirmative)
      {
         await OnLoginClicked();
      }
   }
}
