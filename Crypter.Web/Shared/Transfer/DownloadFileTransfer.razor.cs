/*
 * Copyright (C) 2022 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commercial license options.
 */

using BlazorDownloadFile;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Transfer.DownloadCiphertext;
using Crypter.Contracts.Features.Transfer.DownloadSignature;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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

      protected override async Task OnDecryptClickedAsync()
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
         var maybeRecipientX25519PrivateKey = DecodeX25519RecipientKey();
         if (maybeRecipientX25519PrivateKey.IsNone)
         {
            return;
         }

         byte[] receiveKey;
         byte[] serverKey;
         try
         {
            var senderX25519PublicKey = PEMString.From(Encoding.UTF8.GetString(Convert.FromBase64String(SenderX25519PublicKey)));
            (receiveKey, serverKey) = DeriveSymmetricKeys(maybeRecipientX25519PrivateKey.ValueUnsafe, senderX25519PublicKey);
         }
         catch (Exception)
         {
            DecryptionInProgress = false;
            ErrorMessage = "Invalid X25519 key format";
            return;
         }

         // Get the signature before downloading the ciphertext
         // Remember, the API will DELETE the ciphertext and it's database records as soon as the ciphertext is downloaded
         var signatureRequest = new DownloadTransferSignatureRequest(TransferId);
         var signatureResponse = await CrypterApiService.DownloadFileSignatureAsync(signatureRequest, UserSessionService.LoggedIn);
         await signatureResponse.DoRightAsync(async x =>
         {
            var signature = Convert.FromBase64String(x.SignatureBase64);
            var ed25519PublicKey = PEMString.From(Encoding.UTF8.GetString(Convert.FromBase64String(x.Ed25519PublicKeyBase64)));

            // Request the ciphertext from the server
            await SetNewDecryptionStatus("Downloading encrypted file");
            var encodedServerDecryptionKey = Convert.ToBase64String(serverKey);
            var ciphertextRequest = new DownloadTransferCiphertextRequest(TransferId, encodedServerDecryptionKey);
            var ciphertextResponse = await CrypterApiService.DownloadFileCiphertextAsync(ciphertextRequest, UserSessionService.LoggedIn);

            ciphertextResponse.DoLeft(y =>
            {
               switch (y)
               {
                  case DownloadTransferCiphertextError.NotFound:
                     ErrorMessage = "File not found";
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
            });

            await ciphertextResponse.DoRightAsync(async y =>
            {
               // Decrypt the ciphertext using the symmetric key from the signature
               await SetNewDecryptionStatus($"Decrypting file");
               var ciphertextBytes = Convert.FromBase64String(y.CipherTextBase64);
               var clientEncryptionIV = Convert.FromBase64String(y.ClientEncryptionIVBase64);
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
            });
         });
      }

      protected async Task DownloadFileAsync()
      {
         await BlazorDownloadFileService.DownloadFile(FileName, DecryptedFile, ContentType);
      }
   }
}
