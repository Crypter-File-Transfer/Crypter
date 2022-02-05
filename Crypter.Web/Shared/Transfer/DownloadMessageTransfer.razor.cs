﻿/*
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

using Crypter.Contracts.Features.Transfer.DownloadCiphertext;
using Crypter.Contracts.Features.Transfer.DownloadSignature;
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
         if (!IsUserRecipient
            && string.IsNullOrEmpty(EncodedX25519PrivateKey))
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
         var signatureRequest = new DownloadTransferSignatureRequest(TransferId);
         var maybeSignature = await TransferService.DownloadMessageSignatureAsync(signatureRequest, requestWithAuth);

         await maybeSignature.DoRightAsync(async right =>
         {
            var signature = Convert.FromBase64String(right.SignatureBase64);
            var ed25519PublicKey = Encoding.UTF8.GetString(Convert.FromBase64String(right.Ed25519PublicKeyBase64));

            // Request the ciphertext from the server
            await SetNewDecryptionStatus("Downloading encrypted message");
            var encodedServerDecryptionKey = Convert.ToBase64String(serverKey);
            var ciphertextRequest = new DownloadTransferCiphertextRequest(TransferId, encodedServerDecryptionKey);
            var maybeCiphertext = await TransferService.DownloadMessageCiphertextAsync(ciphertextRequest, requestWithAuth);

            await maybeCiphertext.MatchVoidAsync(
               left =>
               {
                  switch ((DownloadTransferCiphertextError)left.ErrorCode)
                  {
                     case DownloadTransferCiphertextError.NotFound:
                        ErrorMessage = "Message not found";
                        DecryptionInProgress = false;
                        break;
                     case DownloadTransferCiphertextError.ServerDecryptionFailed:
                        ErrorMessage = "Failed to remove server-side encryption";
                        DecryptionInProgress = false;
                        break;
                     default:
                        ErrorMessage = "";
                        DecryptionInProgress = false;
                        break;
                  }
                  return Task.CompletedTask;
               },
               async right =>
               {
                  // Decrypt the ciphertext using the symmetric key from the signature
                  await SetNewDecryptionStatus("Decrypting message");
                  var ciphertextBytes = Convert.FromBase64String(right.CipherTextBase64);
                  var clientEncryptionIV = Convert.FromBase64String(right.ClientEncryptionIVBase64);
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
               });
         });
      }
   }
}
