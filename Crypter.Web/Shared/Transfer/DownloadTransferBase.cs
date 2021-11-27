using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Enums;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Org.BouncyCastle.Crypto;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public abstract class DownloadTransferBase : ComponentBase
   {
      [Inject]
      protected ILocalStorageService LocalStorageService { get; set; }

      [Inject]
      protected ITransferApiService TransferService { get; set; }

      [Parameter]
      public Guid TransferId { get; set; }

      [Parameter]
      public Guid SenderId { get; set; }

      [Parameter]
      public string SenderUsername { get; set; }

      [Parameter]
      public string SenderAlias { get; set; }

      [Parameter]
      public string SenderX25519PublicKey { get; set; }

      [Parameter]
      public Guid RecipientId { get; set; }

      [Parameter]
      public string Created { get; set; }

      [Parameter]
      public int Size { get; set; }

      [Parameter]
      public string Expiration { get; set; }

      [Parameter]
      public EventCallback<Guid> TransferIdChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> SenderIdChanged { get; set; }

      [Parameter]
      public EventCallback<string> SenderUsernameChanged { get; set; }

      [Parameter]
      public EventCallback<string> SenderAliasChanged { get; set; }

      [Parameter]
      public EventCallback<string> SenderX25519PublicKeyChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> RecipientIdChanged { get; set; }

      [Parameter]
      public EventCallback<string> CreatedChanged { get; set; }

      [Parameter]
      public EventCallback<int> SizeChanged { get; set; }

      [Parameter]
      public EventCallback<string> ExpirationChanged { get; set; }

      protected string EncodedX25519PrivateKey;

      protected bool DecryptionInProgress = false;
      protected bool DecryptionCompleted = false;
      protected string DecryptionStatusMessage = "";
      protected string ErrorMessage = "";
      protected bool IsUserRecipient = false;

      protected override async Task OnParametersSetAsync()
      {
         if (!LocalStorageService.HasItem(StoredObjectType.UserSession))
         {
            IsUserRecipient = false;
            return;
         }
         var session = await LocalStorageService.GetItem<UserSession>(StoredObjectType.UserSession);
         IsUserRecipient = RecipientId.Equals(session.UserId);
      }

      protected abstract Task OnDecryptClicked();

      protected async Task<(bool Success, string RecipientX25519PrivateKey)> DecodeX25519RecipientKey()
      {
         if (IsUserRecipient)
         {
            return (true, await LocalStorageService.GetItem<string>(StoredObjectType.PlaintextX25519PrivateKey));
         }

         try
         {
            byte[] bytes = Convert.FromBase64String(EncodedX25519PrivateKey);
            var pem = Encoding.UTF8.GetString(bytes);
            return (true, pem);
         }
         catch (FormatException)
         {
            DecryptionInProgress = false;
            ErrorMessage = "Invalid key format";
            return (false, null);
         }
      }

      protected static (byte[] ReceiveKey, byte[] ServerKey) DeriveSymmetricKeys(string recipientX25519PrivatePEM, string senderX25519PublicPEM)
      {
         var recipientX25519Private = KeyConversion.ConvertX25519PrivateKeyFromPEM(recipientX25519PrivatePEM);
         var recipientX25519Public = recipientX25519Private.GeneratePublicKey();
         var recipientKeyPair = new AsymmetricCipherKeyPair(recipientX25519Public, recipientX25519Private);

         var senderX25519Public = KeyConversion.ConvertX25519PublicKeyFromPEM(senderX25519PublicPEM);
         (var receiveKey, var sendKey) = ECDH.DeriveSharedKeys(recipientKeyPair, senderX25519Public);
         var digestor = new SHA(SHAFunction.SHA256);
         digestor.BlockUpdate(sendKey);
         var serverEncryptionKey = CommonCrypto.DeriveSharedKeyFromECDHDerivedKeys(receiveKey, sendKey);

         return (receiveKey, serverEncryptionKey);
      }

      protected static byte[] DecryptBytes(byte[] ciphertext, byte[] symmetricKey, byte[] symmetricIV)
      {
         var symmetricEncryption = new AES();
         symmetricEncryption.Initialize(symmetricKey, symmetricIV, false);
         return symmetricEncryption.ProcessFinal(ciphertext);
      }

      protected static bool VerifySignature(byte[] plaintext, byte[] signature, string ed25519PublicKey)
      {
         var ed25519PublicDecoded = KeyConversion.ConvertEd25519PublicKeyFromPEM(ed25519PublicKey);

         var verifier = new ECDSA();
         verifier.InitializeVerifier(ed25519PublicDecoded);
         verifier.VerifierDigestChunk(plaintext);
         return verifier.VerifySignature(signature);
      }

      protected async Task SetNewDecryptionStatus(string status)
      {
         DecryptionStatusMessage = status;
         StateHasChanged();
         await Task.Delay(400);
      }
   }
}
