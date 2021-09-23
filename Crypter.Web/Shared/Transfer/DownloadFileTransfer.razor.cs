using Blazor.DownloadFileFast.Interfaces;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Microsoft.AspNetCore.Components;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public partial class DownloadFileTransferBase : DownloadTransferBase
   {
      [Inject]
      protected IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

      [Parameter]
      public string FileName { get; set; }

      [Parameter]
      public string ContentType { get; set; }

      [Parameter]
      public EventCallback<string> FileNameChanged { get; set; }

      [Parameter]
      public EventCallback<string> ContentTypeChanged { get; set; }

      protected byte[] DecryptedFile;

      protected override async Task OnDecryptClicked()
      {
         DecryptionInProgress = true;
         ErrorMessage = "";

         await SetNewDecryptionStatus("Decoding keys");
         (var decodeSuccess, var recipientX25519PrivateKeyPEM) = DecodeX25519RecipientKey();
         if (!decodeSuccess)
         {
            return;
         }

         byte[] receiveKey;
         byte[] serverKey;
         try
         {
            var senderX25519PublicKeyPEM = Encoding.UTF8.GetString(Convert.FromBase64String(SenderX25519PublicKey));
            (receiveKey, serverKey) = DeriveSymmetricKeys(recipientX25519PrivateKeyPEM, senderX25519PublicKeyPEM);
         }
         catch (Exception)
         {
            DecryptionInProgress = false;
            ErrorMessage = "Invalid X25519 key format";
            return;
         }

         // Get the signature before downloading the ciphertext
         // Remember, the API will DELETE the ciphertext and it's database records as soon as the ciphertext is downloaded
         var requestWithAuth = AuthenticationService.User is not null;
         var signatureRequest = new GetTransferSignatureRequest(TransferId);
         var (_, signatureResponse) = await TransferService.DownloadFileSignatureAsync(signatureRequest, requestWithAuth);
         byte[] signature = Convert.FromBase64String(signatureResponse.SignatureBase64);
         string ed25519PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(signatureResponse.Ed25519PublicKeyBase64));

         // Request the ciphertext from the server
         await SetNewDecryptionStatus("Downloading encrypted file");
         var encodedServerDecryptionKey = Convert.ToBase64String(serverKey);
         var ciphertextRequest = new GetTransferCiphertextRequest(TransferId, encodedServerDecryptionKey);
         var (_, ciphertextResponse) = await TransferService.DownloadFileCiphertextAsync(ciphertextRequest, requestWithAuth);

         // Error handler
         switch (ciphertextResponse.Result)
         {
            case DownloadCiphertextResult.Success:
               break;
            case DownloadCiphertextResult.NotFound:
               ErrorMessage = "Message not found";
               DecryptionInProgress = false;
               return;
            case DownloadCiphertextResult.ServerDecryptionFailed:
               ErrorMessage = "Failed to remove server-side encryption";
               DecryptionInProgress = false;
               return;
            default:
               ErrorMessage = "";
               DecryptionInProgress = false;
               return;
         }

         // Decrypt the ciphertext using the symmetric key from the signature
         await SetNewDecryptionStatus($"Decrypting file");
         var ciphertextBytes = Convert.FromBase64String(ciphertextResponse.CipherTextBase64);
         var clientEncryptionIV = Convert.FromBase64String(ciphertextResponse.ClientEncryptionIVBase64);
         var plaintextBytes = DecryptBytes(ciphertextBytes, receiveKey, clientEncryptionIV);

         await SetNewDecryptionStatus("Verifying decrypted file");
         if (VerifySignature(plaintextBytes, signature, ed25519PublicKey))
         {
            DecryptedFile = plaintextBytes;
            DecryptionCompleted = true;
         }
         else
         {
            ErrorMessage = "Failed to verify decrypted file";
         }

         DecryptionInProgress = false;
      }

      protected async Task DownloadFile(string fileName, byte[] fileContent, string contentType)
      {
         await BlazorDownloadFileService.DownloadFileAsync(fileName, fileContent, contentType);
      }
   }
}
