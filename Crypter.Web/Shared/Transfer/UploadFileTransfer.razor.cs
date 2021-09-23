using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public partial class UploadFileTransferBase : UploadTransferBase
   {
      // Constants
      protected const int MaxFileSizeMB = 10;
      protected const int MaxFileSize = MaxFileSizeMB * 1024 * 1024;
      protected const int MaxFileCount = 1;
      protected const int FileReadBlockSize = 1048576;
      protected const string ProgressTransition = "0.20s";

      // State
      protected bool ShowProgressBar = false;
      protected double ProgressPercent = 0.0;
      protected string DropClass = "";
      protected List<string> ErrorMessages = new();

      // User input
      protected IBrowserFile SelectedFile;

      protected void HandleDragEnter()
      {
         DropClass = "dropzone-drag";
      }

      protected void HandleDragLeave()
      {
         DropClass = "";
      }

      protected void HandleFileInputChange(InputFileChangeEventArgs e)
      {
         DropClass = "";
         ErrorMessages.Clear();

         var file = e.File;

         if (file == null)
         {
            ErrorMessages.Add("No file selected.");
            return;
         }

         if (file.Size > MaxFileSize)
         {
            ErrorMessages.Add($"The max file size is {MaxFileSizeMB} MB.");
            return;
         }

         SelectedFile = file;
      }

      protected override async Task OnEncryptClicked()
      {
         if (SelectedFile is null)
         {
            ErrorMessages.Add("No file selected");
            return;
         }

         EncryptionInProgress = true;
         ErrorMessages.Clear();

         await SetNewEncryptionStatus("Generating keys");
         GenerateMissingAsymmetricKeys();
         (var sendKey, var serverKey) = DeriveSymmetricKeys(SenderX25519PrivateKey, RecipientX25519PublicKey);
         var iv = AES.GenerateIV();

         await SetNewEncryptionStatus("Encrypting your file");
         var ciphertext = await EncryptBytesAsync(SelectedFile, sendKey, iv);
         await HideEncryptionProgress();

         await SetNewEncryptionStatus("Signing your file");
         var signature = await SignBytesAsync(SelectedFile, SenderEd25519PrivateKey);
         await HideEncryptionProgress();

         await SetNewEncryptionStatus("Uploading");
         var encodedCipherText = Convert.ToBase64String(ciphertext);
         var encodedECDHSenderKey = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(KeyConversion.ConvertX25519PrivateKeyFromPEM(SenderX25519PrivateKey).GeneratePublicKey().ConvertToPEM()));
         var encodedECDSASenderKey = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(KeyConversion.ConvertEd25519PrivateKeyFromPEM(SenderEd25519PrivateKey).GeneratePublicKey().ConvertToPEM()));
         var encodedServerEncryptionKey = Convert.ToBase64String(serverKey);
         var encodedSignature = Convert.ToBase64String(signature);
         var fileType = string.IsNullOrEmpty(SelectedFile.ContentType)
            ? "application/unknown"
            : SelectedFile.ContentType;
         var encodedClientIV = Convert.ToBase64String(iv);

         var withAuth = AuthenticationService.User is not null;
         var request = new FileTransferRequest(SelectedFile.Name, fileType, encodedCipherText, encodedSignature, encodedClientIV, encodedServerEncryptionKey, encodedECDHSenderKey, encodedECDSASenderKey);
         var (_, response) = await UploadService.UploadFileTransferAsync(request, RecipientId, withAuth);

         switch (response.Result)
         {
            case UploadResult.BlockedByUserPrivacy:
               ErrorMessages.Add("This user does not accept files.");
               EncryptionInProgress = false;
               return;
            case UploadResult.OutOfSpace:
               ErrorMessages.Add("The server is full. Try again later.");
               EncryptionInProgress = false;
               return;
            default:
               break;
         }

         TransferId = response.Id;

         if (RecipientId == default)
         {
            ModalForAnonymousRecipient.Open();
         }
         else
         {
            ModalForUserRecipient.Open();
         }

         EncryptionInProgress = false;
         Cleanup();
      }

      protected async Task<byte[]> EncryptBytesAsync(IBrowserFile plaintext, byte[] symmetricKey, byte[] symmetricIV)
      {
         await SetProgressBar(0.0);
         var symmetricEncryption = new AES();
         symmetricEncryption.Initialize(symmetricKey, symmetricIV, true);

         using var fileStream = plaintext.OpenReadStream(MaxFileSize);
         var fileSize = (int)plaintext.Size;
         int processedBytes = 0;
         int currentCiphertextSize = 0;
         List<byte> ciphertext = new(symmetricEncryption.GetOutputSize(fileSize));

         while (processedBytes + FileReadBlockSize < fileSize)
         {
            var plaintextChunk = new byte[FileReadBlockSize];
            await fileStream.ReadAsync(plaintextChunk.AsMemory(0, plaintextChunk.Length));
            var ciphertextChunk = symmetricEncryption.ProcessChunk(plaintextChunk);

            ciphertext.InsertRange(currentCiphertextSize, ciphertextChunk);
            processedBytes += FileReadBlockSize;
            currentCiphertextSize += ciphertextChunk.Length;
            await SetProgressBar((double)processedBytes / fileSize);
         }

         var finalPlaintextChunk = new byte[fileSize - processedBytes];
         await fileStream.ReadAsync(finalPlaintextChunk.AsMemory(0, finalPlaintextChunk.Length));
         var finalCiphertextChunk = symmetricEncryption.ProcessFinal(finalPlaintextChunk);
         ciphertext.InsertRange(currentCiphertextSize, finalCiphertextChunk);

         await SetProgressBar(1.0);
         return ciphertext.ToArray();
      }

      protected async Task<byte[]> SignBytesAsync(IBrowserFile file, string ed25519PrivateKey)
      {
         await SetProgressBar(0.0);
         var ed25519PrivateDecoded = KeyConversion.ConvertEd25519PrivateKeyFromPEM(ed25519PrivateKey);
         var signer = new ECDSA();
         signer.InitializeSigner(ed25519PrivateDecoded);

         using var fileStream = file.OpenReadStream(MaxFileSize);
         var fileSize = (int)file.Size;
         int processedBytes = 0;
         while (processedBytes + FileReadBlockSize < fileSize)
         {
            var plaintextChunk = new byte[FileReadBlockSize];
            await fileStream.ReadAsync(plaintextChunk.AsMemory(0, plaintextChunk.Length));

            signer.SignerDigestChunk(plaintextChunk);

            processedBytes += FileReadBlockSize;
            await SetProgressBar((double)processedBytes / fileSize);
         }

         var finalPlaintextChunk = new byte[fileSize - processedBytes];
         await fileStream.ReadAsync(finalPlaintextChunk.AsMemory(0, finalPlaintextChunk.Length));
         signer.SignerDigestChunk(finalPlaintextChunk);
         var signature = signer.GenerateSignature();
         await SetProgressBar(1.0);
         return signature;
      }

      protected override void Cleanup()
      {
         SelectedFile = null;

         base.Cleanup();
      }

      protected async Task SetProgressBar(double percentComplete)
      {
         ShowProgressBar = true;
         ProgressPercent = percentComplete;
         StateHasChanged();
         await Task.Delay(250);
      }

      protected async Task HideEncryptionProgress()
      {
         ShowProgressBar = false;
         StateHasChanged();
         await Task.Delay(400);
      }
   }
}
