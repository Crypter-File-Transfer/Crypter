using Blazor.DownloadFileFast.Interfaces;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
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
   public partial class DecryptFileDetailsBase : ComponentBase
   {
      [Inject]
      protected IBlazorDownloadFileService BlazorDownloadFileService { get; set; }

      [Inject]
      protected IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      protected IDownloadService DownloadService { get; set; }

      [Parameter]
      public Guid Id { get; set; }

      [Parameter]
      public string FileName { get; set; }

      [Parameter]
      public string ContentType { get; set; }

      [Parameter]
      public Guid SenderId { get; set; }

      [Parameter]
      public string SenderUsername { get; set; }

      [Parameter]
      public string SenderPublicAlias { get; set; }

      [Parameter]
      public Guid RecipientId { get; set; }

      [Parameter]
      public string Created { get; set; }

      [Parameter]
      public int Size { get; set; }

      [Parameter]
      public string Expiration { get; set; }

      [Parameter]
      public EventCallback<Guid> IdChanged { get; set; }

      [Parameter]
      public EventCallback<string> FileNameChanged { get; set; }

      [Parameter]
      public EventCallback<string> ContentTypeChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> SenderIdChanged { get; set; }

      [Parameter]
      public EventCallback<string> SenderUsernameChanged { get; set; }

      [Parameter]
      public EventCallback<string> SenderPublicAliasChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> RecipientIdChanged { get; set; }

      [Parameter]
      public EventCallback<string> CreatedChanged { get; set; }

      [Parameter]
      public EventCallback<int> SizeChanged { get; set; }

      [Parameter]
      public EventCallback<string> ExpirationChanged { get; set; }

      protected bool UserIsRecipient;
      protected bool IsDecrypting;
      protected string DecryptionStatusMessage;
      protected string DecryptionKey;
      protected bool DecryptionSuccess;
      protected bool DecryptionError;
      protected string DecryptionErrorText;
      protected byte[] DecryptedFile;

      protected override async Task OnInitializedAsync()
      {
         await base.OnInitializedAsync();
         UserIsRecipient = RecipientId == AuthenticationService.User?.Id;
         if (UserIsRecipient)
         {
            UserIsRecipient = true;
            await OnDecryptClicked(Id, AuthenticationService.User.PrivateKey, true);
         }
      }

      protected async Task OnDecryptClicked(Guid id, string privateKey, bool privateKeyAlreadyDecoded)
      {
         IsDecrypting = true;
         DecryptionError = false;

         string privatePemKey;
         if (privateKeyAlreadyDecoded)
         {
            privatePemKey = privateKey;
         }
         else
         {
            // Attempt to convert the decryption key from Base64
            await SetNewDecryptionStatus("Decoding key");
            try
            {
               byte[] pemKeyAsBytes = Convert.FromBase64String(privateKey);
               privatePemKey = Encoding.UTF8.GetString(pemKeyAsBytes);
            }
            catch (FormatException)
            {
               DecryptionError = true;
               IsDecrypting = false;
               DecryptionErrorText = "Invalid key format";
               return;
            }
         }

         await SetNewDecryptionStatus("Downloading signature and encrypted symmetric details");
         var makeRequestsWithAuth = AuthenticationService.User is not null;
         var signatureRequest = new GenericSignatureRequest(id);
         var (_, signatureResponse) = await DownloadService.DownloadFileSignatureAsync(signatureRequest, makeRequestsWithAuth);
         byte[] signature = Convert.FromBase64String(signatureResponse.SignatureBase64);
         byte[] encryptedSymmetricInfo = Convert.FromBase64String(signatureResponse.EncryptedSymmetricInfoBase64);
         string publicPemKey = Encoding.UTF8.GetString(Convert.FromBase64String(signatureResponse.PublicKeyBase64));

         // Attempt to decrypt the signature using the provided key
         await SetNewDecryptionStatus("Decrypting symmetric details");
         SymmetricInfoDTO symmetricInfo;
         try
         {
            symmetricInfo = CryptoLib.Common.DecryptAndDeserializeSymmetricInfo(encryptedSymmetricInfo, privatePemKey);
         }
         catch (FormatException)
         {
            DecryptionError = true;
            IsDecrypting = false;
            DecryptionErrorText = "Failed to decrypt symmetric details";
            return;
         }

         // Request the ciphertext from the server
         await SetNewDecryptionStatus("Downloading file");
         var serverDecryptionKey = CryptoLib.Common.GetDigest(symmetricInfo.Key, DigestAlgorithm.SHA256);
         var encodedServerDecryptionKey = Convert.ToBase64String(serverDecryptionKey);
         var ciphertextRequest = new GenericCiphertextRequest(id, encodedServerDecryptionKey);
         var (_, ciphertextResponse) = await DownloadService.DownloadFileCiphertextAsync(ciphertextRequest, makeRequestsWithAuth);

         // Error handler
         switch (ciphertextResponse.Result)
         {
            case DownloadCiphertextResult.Success:
               break;
            case DownloadCiphertextResult.NotFound:
               DecryptionErrorText = "Message not found";
               DecryptionError = true;
               IsDecrypting = false;
               return;
            case DownloadCiphertextResult.ServerDecryptionFailed:
               DecryptionErrorText = "Failed to remove server-side encryption";
               DecryptionError = true;
               IsDecrypting = false;
               return;
            default:
               DecryptionError = true;
               IsDecrypting = false;
               return;
         }

         // Decrypt the ciphertext using the symmetric key from the signature
         await SetNewDecryptionStatus($"Decrypting file");
         byte[] ciphertextBytes = Convert.FromBase64String(ciphertextResponse.CipherTextBase64);
         var symmetricParams = CryptoLib.Common.MakeSymmetricCryptoParams(symmetricInfo.Key, symmetricInfo.IV);
         byte[] plaintextBytes = CryptoLib.Common.UndoSymmetricEncryption(ciphertextBytes, symmetricParams);

         await SetNewDecryptionStatus("Verifying decrypted file");
         DecryptionSuccess = CryptoLib.Common.VerifySignature(plaintextBytes, signature, publicPemKey);

         if (DecryptionSuccess)
         {
            DecryptedFile = plaintextBytes;
         }
         else
         {
            DecryptionError = true;
            IsDecrypting = false;
            DecryptionErrorText = "Failed to verify decrypted file";
         }
      }

      protected async Task DownloadFile(string fileName, byte[] fileContent, string contentType)
      {
         await BlazorDownloadFileService.DownloadFileAsync(fileName, fileContent, contentType);
      }

      protected async Task SetNewDecryptionStatus(string status)
      {
         DecryptionStatusMessage = status;
         StateHasChanged();
         await Task.Delay(500);
      }
   }
}
