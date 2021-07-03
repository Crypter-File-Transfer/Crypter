using BlazorInputFile;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Enums;
using Crypter.CryptoLib.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class FileUploadBase : ComponentBase
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
      protected const int MaxFileSizeMB = 10;
      protected const int MaxFileSize = MaxFileSizeMB * 1024 * 1024;
      protected const int MaxFileCount = 1;

      // State
      protected bool IsEncrypting = false;
      protected string EncryptionStatusMessage = "";

      // User input
      protected List<IFileListEntry> SelectedFiles = new();
      protected CryptoStrength encryptionStrength = CryptoStrength.Minimum;

      protected string dropClass = "";
      protected List<string> ErrorMessages = new();
      protected Guid returnedId;
      protected string privateKey;

      // Generated keys and crypto params
      protected WrapsAsymmetricCipherKeyPair asymmetricKeyPair;
      protected SymmetricCryptoParams symmetricParams;
      protected byte[] serverEncryptionKey;

      protected string UploadSuccessForUserMessage = "Your file has been encrypted and uploaded." +
         " The recipient will see the file during their next login." +
         "\nUploads automatically expire in 24 hours.";

      protected void HandleDragEnter()
      {
         dropClass = "dropzone-drag";
      }

      protected void HandleDragLeave()
      {
         dropClass = "";
      }

      protected void HandleFileInputChange(IFileListEntry[] files)
      {
         dropClass = "";
         ErrorMessages.Clear();

         if (!files.Any())
         {
            ErrorMessages.Add("No file selected.");
            return;
         }

         if (files.Length > MaxFileCount || SelectedFiles.Count >= MaxFileCount)
         {
            ErrorMessages.Add($"You can only upload {MaxFileCount} file(s) at a time.");
            return;
         }

         if (files.Any(x => x.Size > MaxFileSize))
         {
            ErrorMessages.Add($"The max file size is {MaxFileSizeMB} MB.");
            return;
         }

         SelectedFiles.Add(files[0]);
      }

      protected async Task OnEncryptFileClicked(List<IFileListEntry> files, CryptoStrength strength)
      {
         if (files.Count == 1)
         {
            IsEncrypting = true;
            await GenerateKeys(strength);
            GC.Collect();
            await Task.Delay(500);
            await EncryptFile(files[0]);
            GC.Collect();
            ClearKeys();
            GC.Collect();
         }
         else
         {
            ErrorMessages.Add("No file selected");
         }
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

      protected async Task EncryptFile(IFileListEntry file)
      {
         ErrorMessages.Clear();
         await SetNewEncryptionStatus("Encrypting your file");
         using var stream = new MemoryStream();
         await file.Data.CopyToAsync(stream);
         byte[] fileBytes = stream.ToArray();
         var cipherText = CryptoLib.Common.DoSymmetricEncryption(fileBytes, symmetricParams);

         await SetNewEncryptionStatus("Encrypting symmetric key");
         var publicKeyToEncryptWith = string.IsNullOrEmpty(RecipientUsername)
             ? asymmetricKeyPair.Public
             : CryptoLib.Common.ConvertRsaPublicKeyFromPEM(RecipientPublicKey);

         var encryptedSymmetricInfo = CryptoLib.Common.EncryptSymmetricInfo(symmetricParams, publicKeyToEncryptWith);

         await SetNewEncryptionStatus("Creating a signature");
         var signature = CryptoLib.Common.SignPlaintext(fileBytes, asymmetricKeyPair.Private);

         await SetNewEncryptionStatus("Preparing to upload");
         var encodedCipherText = Convert.ToBase64String(cipherText);
         var encodedSymmetricInfo = Convert.ToBase64String(encryptedSymmetricInfo);
         var encodedServerEncryptionKey = Convert.ToBase64String(serverEncryptionKey);
         var encodedSignature = Convert.ToBase64String(signature);
         var encodedPublicKey = Convert.ToBase64String(
             Encoding.UTF8.GetBytes(asymmetricKeyPair.Public.ConvertToPEM()));
         var fileType = string.IsNullOrEmpty(file.Type)
            ? "application/unknown"
            : file.Type;

         await SetNewEncryptionStatus("Uploading");
         var withAuth = AuthenticationService.User is not null;
         var request = new FileUploadRequest(file.Name, fileType, encodedCipherText, encodedSymmetricInfo, encodedSignature, encodedServerEncryptionKey, encodedPublicKey, RecipientUsername);
         var (_, response) = await UploadService.UploadFileAsync(request, withAuth);

         switch (response.Result)
         {
            case UploadResult.BlockedByUserPrivacy:
               ErrorMessages.Add("This user does not accept files.");
               IsEncrypting = false;
               return;
            case UploadResult.OutOfSpace:
               ErrorMessages.Add("The server is full.  Try again later.");
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

         SelectedFiles.Clear();
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
