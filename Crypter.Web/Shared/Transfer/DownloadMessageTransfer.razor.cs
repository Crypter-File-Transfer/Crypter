using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public partial class DownloadMessageTransferBase : DownloadTransferBase
   {
      [Parameter]
      public string Subject { get; set; }

      [Parameter]
      public EventCallback<string> SubjectChanged { get; set; }

      protected string DecryptedMessage;

      protected override async Task OnDecryptClicked()
      {
         if (string.IsNullOrEmpty(EncodedX25519PrivateKey))
         {
            DecryptionInProgress = false;
            ErrorMessage = "You must enter a decryption key";
            return;
         }

         DecryptionInProgress = true;
         ErrorMessage = "";

         await SetNewDecryptionStatus("Decoding keys");
         (var decodeSuccess, var recipientX25519PrivateKeyPEM) = await DecodeX25519RecipientKey();
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
         var requestWithAuth = LocalStorageService.HasItem(StoredObjectType.UserSession);
         var signatureRequest = new GetTransferSignatureRequest(TransferId);
         var (_, signatureResponse) = await TransferService.DownloadMessageSignatureAsync(signatureRequest, requestWithAuth);
         byte[] signature = Convert.FromBase64String(signatureResponse.SignatureBase64);
         string ed25519PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(signatureResponse.Ed25519PublicKeyBase64));

         // Request the ciphertext from the server
         await SetNewDecryptionStatus("Downloading encrypted message");
         var encodedServerDecryptionKey = Convert.ToBase64String(serverKey);
         var ciphertextRequest = new GetTransferCiphertextRequest(TransferId, encodedServerDecryptionKey);
         var (_, ciphertextResponse) = await TransferService.DownloadMessageCiphertextAsync(ciphertextRequest, requestWithAuth);

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
         await SetNewDecryptionStatus("Decrypting message");
         var ciphertextBytes = Convert.FromBase64String(ciphertextResponse.CipherTextBase64);
         var clientEncryptionIV = Convert.FromBase64String(ciphertextResponse.ClientEncryptionIVBase64);
         var plaintextBytes = DecryptBytes(ciphertextBytes, receiveKey, clientEncryptionIV);

         await SetNewDecryptionStatus("Verifying decrypted message");
         if (VerifySignature(plaintextBytes, signature, ed25519PublicKey))
         {
            DecryptedMessage = Encoding.UTF8.GetString(plaintextBytes);
            DecryptionCompleted = true;
         }
         else
         {
            ErrorMessage = "Failed to verify decrypted file";
         }

         DecryptionInProgress = false;
      }
   }
}
