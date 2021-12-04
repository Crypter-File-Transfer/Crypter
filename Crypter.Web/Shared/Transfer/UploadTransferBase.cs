/*
 * Copyright (C) 2021 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Enums;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Org.BouncyCastle.Crypto;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public abstract class UploadTransferBase : ComponentBase
   {
      [Inject]
      protected ILocalStorageService LocalStorageService { get; set; }

      [Inject]
      protected ITransferApiService UploadService { get; set; }

      [Parameter]
      public bool IsSenderDefined { get; set; }

      [Parameter]
      public string SenderX25519PrivateKey { get; set; }

      [Parameter]
      public string SenderEd25519PrivateKey { get; set; }

      [Parameter]
      public bool IsRecipientDefined { get; set; }

      [Parameter]
      public Guid RecipientId { get; set; }

      [Parameter]
      public string RecipientX25519PrivateKey { get; set; }

      [Parameter]
      public string RecipientX25519PublicKey { get; set; }

      [Parameter]
      public string RecipientEd25519PublicKey { get; set; }

      [Parameter]
      public EventCallback<bool> IsSenderDefinedChanged { get; set; }

      [Parameter]
      public EventCallback<string> SenderX25519PrivateKeyChanged { get; set; }

      [Parameter]
      public EventCallback<string> SenderEd25519PrivateKeyChanged { get; set; }

      [Parameter]
      public EventCallback<bool> IsRecipientDefinedChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> RecipientIdChanged { get; set; }

      [Parameter]
      public EventCallback<Guid> RecipientX25519PrivateKeyChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientX25519PublicKeyChanged { get; set; }

      [Parameter]
      public EventCallback<string> RecipientEd25519PublicKeyChanged { get; set; }

      [Parameter]
      public EventCallback UploadCompletedEvent { get; set; }

      protected Modal.TransferSuccessModal ModalForAnonymousRecipient { get; set; }
      protected Modal.BasicModal ModalForUserRecipient { get; set; }

      protected bool EncryptionInProgress = false;
      protected string EncryptionStatusMessage = "";

      protected Guid TransferId;
      protected string EncodedRecipientX25519PrivateKey;

      protected string UploadSuccessForUserMessage = "Your file has been encrypted and uploaded." +
         " The recipient will see the file during their next login." +
         "\nUploads automatically expire after 24 hours.";

      protected abstract Task OnEncryptClicked();

      protected void GenerateMissingAsymmetricKeys()
      {
         if (!IsSenderDefined)
         {
            SenderX25519PrivateKey = ECDH.GenerateKeys().Private.ConvertToPEM();
            SenderEd25519PrivateKey = ECDSA.GenerateKeys().Private.ConvertToPEM();
         }

         if (!IsRecipientDefined)
         {
            var recipientX25519Keys = ECDH.GenerateKeys();
            RecipientX25519PrivateKey = recipientX25519Keys.Private.ConvertToPEM();
            RecipientX25519PublicKey = recipientX25519Keys.Public.ConvertToPEM();
            RecipientEd25519PublicKey = ECDSA.GenerateKeys().Public.ConvertToPEM();
            EncodedRecipientX25519PrivateKey = Convert.ToBase64String(Encoding.UTF8.GetBytes(RecipientX25519PrivateKey));
         }
      }

      protected static (byte[] SendKey, byte[] ServerKey) DeriveSymmetricKeys(string senderX25519PrivateKey, string recipientX25519PublicKey)
      {
         var senderX25519PrivateDecoded = KeyConversion.ConvertX25519PrivateKeyFromPEM(senderX25519PrivateKey);
         var senderX25519PublicDecoded = senderX25519PrivateDecoded.GeneratePublicKey();
         var senderKeyPair = new AsymmetricCipherKeyPair(senderX25519PublicDecoded, senderX25519PrivateDecoded);
         var recipientX25519PublicDecoded = KeyConversion.ConvertX25519PublicKeyFromPEM(recipientX25519PublicKey);
         (var receiveKey, var sendKey) = ECDH.DeriveSharedKeys(senderKeyPair, recipientX25519PublicDecoded);
         var digestor = new SHA(SHAFunction.SHA256);
         digestor.BlockUpdate(sendKey);
         var serverEncryptionKey = CommonCrypto.DeriveSharedKeyFromECDHDerivedKeys(receiveKey, sendKey);

         return (sendKey, serverEncryptionKey);
      }

      protected async Task SetNewEncryptionStatus(string status)
      {
         EncryptionStatusMessage = status;
         StateHasChanged();
         await Task.Delay(400);
      }

      protected virtual void Cleanup()
      {
         if (!IsSenderDefined)
         {
            SenderEd25519PrivateKey = null;
            SenderX25519PrivateKey = null;
         }

         if (!IsRecipientDefined)
         {
            RecipientX25519PrivateKey = null;
            RecipientX25519PublicKey = null;
            RecipientEd25519PublicKey = null;
         }
      }
   }
}
