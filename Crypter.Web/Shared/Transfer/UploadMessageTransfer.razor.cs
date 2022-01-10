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
         var requestedExpiration = RequestedExpirationService.ReturnRequestedExpirationFromRequestedExpirationInHours(RequestedExpirationInHours);
         var request = new MessageTransferRequest(MessageSubject, encodedCipherText, encodedSignature, encodedClientIV, encodedServerEncryptionKey, encodedECDHSenderKey, encodedECDSASenderKey, requestedExpiration);
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
