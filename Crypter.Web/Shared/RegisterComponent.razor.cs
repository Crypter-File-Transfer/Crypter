using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Web.Models.Forms;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class RegisterComponentBase : ComponentBase
   {
      [Inject]
      protected NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected IUserService UserService { get; set; }

      protected UserRegistration RegistrationInfo = new();

      protected bool RegistrationError = false;
      protected string RegistrationErrorText = "";
      protected bool RegistrationSuccess = false;

      protected string IsValid = "is-valid";
      protected string IsInvalid = "is-invalid";

      protected string UsernameInvalidClass = "";
      protected string UsernameValidationMessage;
      private readonly static string MissingUsername = "Please choose a username";
      private readonly static string UsernameTooLong = "Username exceeds 32-character limit";
      private readonly static string UsernameContainsSpaces = "Username may not contain spaces";

      protected string PasswordInvalidClass = "";
      protected string PasswordValidationMessage;
      private readonly static string MissingPassword = "Please enter a password";

      protected string PasswordConfirmInvalidClass = "";
      protected string PasswordConfirmValidationMessage;
      private readonly static string MissingPasswordConfirm = "Please confirm your password";
      private readonly static string PasswordConfirmDoesNotMatch = "Passwords do not match";

      protected bool UserProvidedEmailDuringRegistration = false;

      protected override async Task OnInitializedAsync()
      {
         if (AuthenticationService.User != null)
         {
            NavigationManager.NavigateTo("/user");
         }
         await base.OnInitializedAsync();
      }

      protected bool ValidateForm()
      {
         var formIsValid = true;

         if (!ValidateUsername())
         {
            formIsValid = false;
         }

         if (!ValidatePassword())
         {
            formIsValid = false;
         }

         if (!ValidatePasswordConfirmation())
         {
            formIsValid = false;
         }

         return formIsValid;
      }

      protected bool ValidateUsername()
      {
         if (string.IsNullOrEmpty(RegistrationInfo.Username))
         {
            UsernameValidationMessage = MissingUsername;
            UsernameInvalidClass = IsInvalid;
            return false;
         }
         else if (RegistrationInfo.Username.Length > 32)
         {
            UsernameValidationMessage = UsernameTooLong;
            UsernameInvalidClass = IsInvalid;
            return false;
         }
         else if (RegistrationInfo.Username.Contains(" "))
         {
            UsernameValidationMessage = UsernameContainsSpaces;
            UsernameInvalidClass = IsInvalid;
            return false;
         }

         UsernameInvalidClass = "";
         return true;
      }

      protected bool ValidatePassword()
      {
         if (string.IsNullOrEmpty(RegistrationInfo.Password))
         {
            PasswordValidationMessage = MissingPassword;
            PasswordInvalidClass = IsInvalid;
            return false;
         }

         PasswordInvalidClass = "";
         return true;
      }

      protected bool ValidatePasswordConfirmation()
      {
         if (string.IsNullOrEmpty(RegistrationInfo.PasswordConfirm))
         {
            PasswordConfirmValidationMessage = MissingPasswordConfirm;
            PasswordConfirmInvalidClass = IsInvalid;
            return false;
         }
         else if (!RegistrationInfo.Password.Equals(RegistrationInfo.PasswordConfirm))
         {
            PasswordConfirmValidationMessage = PasswordConfirmDoesNotMatch;
            PasswordConfirmInvalidClass = IsInvalid;
            return false;
         }

         PasswordConfirmInvalidClass = "";
         return true;
      }

      protected async Task OnRegisterClickedAsync()
      {
         if (!ValidateForm())
         {
            return;
         }

         byte[] digestedPassword = CryptoLib.UserFunctions.DigestUserCredentials(RegistrationInfo.Username, RegistrationInfo.Password);
         string digestedPasswordBase64 = Convert.ToBase64String(digestedPassword);

         var requestBody = new RegisterUserRequest(RegistrationInfo.Username, digestedPasswordBase64, RegistrationInfo.EmailAddress);
         var (_, registerResponse) = await UserService.RegisterUserAsync(requestBody);

         if (registerResponse.Result != InsertUserResult.Success)
         {
            RegistrationError = true;
            RegistrationErrorText = registerResponse.Result switch
            {
               InsertUserResult.InvalidUsername => "Invalid username",
               InsertUserResult.InvalidPassword => "Invalid password",
               InsertUserResult.InvalidEmailAddress => "Invalid email address",
               InsertUserResult.UsernameTaken => "Username is already taken",
               InsertUserResult.EmailTaken => "Email address is associated with an existing account",
               _ => "???"
            };
         }
         else
         {
            UserProvidedEmailDuringRegistration = !string.IsNullOrEmpty(RegistrationInfo.EmailAddress);
            RegistrationSuccess = true;
         }
      }
   }
}
