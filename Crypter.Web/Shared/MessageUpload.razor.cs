using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Enums;
using Crypter.CryptoLib.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class MessageUploadBase : ComponentBase
   {
      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected IUploadService UploadService { get; set; }

      [Parameter]
      public string RecipientUsername { get; set; }

      [Parameter]
      public string RecipientPublicKey { get; set; }

      [Parameter]
      public EventCallback<string> RecipientUsernameChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientPublicKeyChanged { get; set; }

      [Parameter]
      public EventCallback UploadCompletedEvent { get; set; }

      protected Modal.UploadSuccessModal SuccessModal { get; set; }
      protected Modal.BasicModal ModalForRecipient { get; set; }

      // Constants
      protected const int MaxMessageLength = 1024;

      // State
      protected bool IsEncrypting = false;
      protected string EncryptionStatusMessage = "";
      protected bool Error = false;
      protected string ErrorMessage = "";

      // User input
      protected string MessageText = "";
      protected string MessageSubject = "";
      protected CryptoStrength encryptionStrength = CryptoStrength.Minimum;

      // Variables we control
      protected Guid returnedId;
      protected string privateKey;

      // Generated keys and crypto params
      protected WrapsAsymmetricCipherKeyPair asymmetricKeyPair;
      protected SymmetricCryptoParams symmetricParams;
      protected byte[] serverEncryptionKey;

      protected string UploadSuccessForUserMessage = "Your message has been encrypted and uploaded." +
         " The recipient will see the message during their next login." +
         "\nUploads automatically expire in 24 hours.";

      protected async Task OnEncryptClicked(string message, CryptoStrength strength)
      {
         IsEncrypting = true;
         await GenerateKeys(strength);
         GC.Collect();
         await EncryptMessage(message);
         GC.Collect();
         ClearKeys();
         GC.Collect();
      }

      protected async Task GenerateKeys(CryptoStrength strength)
      {
         await SetNewEncryptionStatus("Creating symmetric key");
         symmetricParams = CryptoLib.Common.GenerateSymmetricCryptoParams(strength);
         serverEncryptionKey = CryptoLib.Common.GetDigest(symmetricParams.Key.ConvertToBytes(), DigestAlgorithm.SHA256);

         await SetNewEncryptionStatus("Creating asymmetric keys. This may take a while.");
         asymmetricKeyPair = CryptoLib.Common.GenerateAsymmetricKeys(strength);
      }

      protected void ClearKeys()
      {
         asymmetricKeyPair = null;
         symmetricParams = null;
         serverEncryptionKey = null;
      }

      protected async Task EncryptMessage(string message)
      {
         await SetNewEncryptionStatus("Creating a signature");
         byte[] messageBytes = Encoding.UTF8.GetBytes(message);
         var signature = CryptoLib.Common.SignPlaintext(messageBytes, asymmetricKeyPair.Private);

         await SetNewEncryptionStatus("Encrypting your message");
         var cipherText = CryptoLib.Common.DoSymmetricEncryption(messageBytes, symmetricParams);

         await SetNewEncryptionStatus("Encrypting symmetric key");
         var publicKeyToEncryptWith = string.IsNullOrEmpty(RecipientUsername)
             ? asymmetricKeyPair.Public
             : CryptoLib.Common.ConvertRsaPublicKeyFromPEM(RecipientPublicKey);

         var encryptedSymmetricInfo = CryptoLib.Common.EncryptSymmetricInfo(symmetricParams, publicKeyToEncryptWith);

         await SetNewEncryptionStatus("Preparing to upload");
         var encodedCipherText = Convert.ToBase64String(cipherText);
         var encodedSymmetricInfo = Convert.ToBase64String(encryptedSymmetricInfo);
         var encodedServerEncryptionKey = Convert.ToBase64String(serverEncryptionKey);
         var encodedSignature = Convert.ToBase64String(signature);
         var encodedPublicKey = Convert.ToBase64String(
             Encoding.UTF8.GetBytes(asymmetricKeyPair.Public.ConvertToPEM()));

         await SetNewEncryptionStatus("Uploading");
         var withAuth = AuthenticationService.User is not null;

         // TODO - Need to sign with the user's private key; not the generated private key
         var request = new MessageUploadRequest(MessageSubject, encodedCipherText, encodedSymmetricInfo, encodedSignature, encodedServerEncryptionKey, encodedPublicKey, RecipientUsername);
         var (_, response) = await UploadService.UploadMessageAsync(request, withAuth);
         switch (response.Result)
         {
            case UploadResult.BlockedByUserPrivacy:
               Error = true;
               ErrorMessage = "This user does not accept messages.";
               IsEncrypting = false;
               return;
            case UploadResult.OutOfSpace:
               Error = true;
               ErrorMessage = "The server is full.  Try again later.";
               IsEncrypting = false;
               return;
            default:
               break;
         }

         CompletedUpload(response.Id, Convert.ToBase64String(Encoding.UTF8.GetBytes(asymmetricKeyPair.Private.ConvertToPEM())));

         if (string.IsNullOrEmpty(RecipientUsername))
         {
            SuccessModal.Open();
         }
         else
         {
            ModalForRecipient.Open();
         }

         MessageSubject = "";
         MessageText = "";
         IsEncrypting = false;
      }

      protected async Task SetNewEncryptionStatus(string status)
      {
         EncryptionStatusMessage = status;
         StateHasChanged();
         await Task.Delay(500);
      }

      protected void CompletedUpload(Guid id, string privKey)
      {
         returnedId = id;
         privateKey = privKey;
      }
   }
}
