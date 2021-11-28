using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Crypter.Web.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public partial class UploadMessageTransferBase : UploadTransferBase
   {
      // Constants
      protected const int MaxMessageLength = 1024;

      // State
      protected bool Error = false;
      protected string ErrorMessage = "";

      // User input
      protected string MessageText = "";
      protected string MessageSubject = "";

      protected override async Task OnEncryptClicked()
      {
         EncryptionInProgress = true;

         await SetNewEncryptionStatus("Generating keys");
         GenerateMissingAsymmetricKeys();
         (var sendKey, var serverKey) = DeriveSymmetricKeys(SenderX25519PrivateKey, RecipientX25519PublicKey);
         var iv = AES.GenerateIV();

         await SetNewEncryptionStatus("Encrypting your message");
         var messageBytes = Encoding.UTF8.GetBytes(MessageText);
         var ciphertext = EncryptBytes(messageBytes, sendKey, iv);

         await SetNewEncryptionStatus("Signing your message");
         var signature = SignPlaintext(messageBytes, SenderEd25519PrivateKey);

         await SetNewEncryptionStatus("Uploading");
         var encodedCipherText = Convert.ToBase64String(ciphertext);
         var encodedECDHSenderKey = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(KeyConversion.ConvertX25519PrivateKeyFromPEM(SenderX25519PrivateKey).GeneratePublicKey().ConvertToPEM()));
         var encodedECDSASenderKey = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(KeyConversion.ConvertEd25519PrivateKeyFromPEM(SenderEd25519PrivateKey).GeneratePublicKey().ConvertToPEM()));
         var encodedServerEncryptionKey = Convert.ToBase64String(serverKey);
         var encodedSignature = Convert.ToBase64String(signature);
         var encodedClientIV = Convert.ToBase64String(iv);

         var withAuth = LocalStorageService.HasItem(StoredObjectType.UserSession);
         var request = new MessageTransferRequest(MessageSubject, encodedCipherText, encodedSignature, encodedClientIV, encodedServerEncryptionKey, encodedECDHSenderKey, encodedECDSASenderKey);
         var (_, response) = await UploadService.UploadMessageTransferAsync(request, RecipientId, withAuth);

         switch (response.Result)
         {
            case UploadResult.BlockedByUserPrivacy:
               Error = true;
               ErrorMessage = "This user does not accept files.";
               EncryptionInProgress = false;
               return;
            case UploadResult.OutOfSpace:
               Error = true;
               ErrorMessage = "The server is full. Try again later.";
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

      protected static byte[] EncryptBytes(byte[] message, byte[] symmetricKey, byte[] symmetricIV)
      {
         var symmetricEncryption = new AES();
         symmetricEncryption.Initialize(symmetricKey, symmetricIV, true);
         return symmetricEncryption.ProcessFinal(message);
      }

      protected static byte[] SignPlaintext(byte[] message, string ed25519PrivateKey)
      {
         var ed25519PrivateDecoded = KeyConversion.ConvertEd25519PrivateKeyFromPEM(ed25519PrivateKey);
         var signer = new ECDSA();
         signer.InitializeSigner(ed25519PrivateDecoded);
         signer.SignerDigestChunk(message);
         return signer.GenerateSignature();
      }

      protected override void Cleanup()
      {
         MessageSubject = "";
         MessageText = "";

         base.Cleanup();
      }
   }
}
