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

using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Transfer.Handlers.Base;
using Crypter.ClientServices.Transfer.Models;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Transfer;
using Crypter.CryptoLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class DownloadFileHandler : DownloadHandler
   {
      private readonly ICompressionService _compressionService;
      private readonly FileTransferSettings _fileTransferSettings;

      public DownloadFileHandler(ICrypterApiService crypterApiService, ISimpleEncryptionService simpleEncryptionService, ISimpleSignatureService simpleSignatureService, IUserSessionService userSessionService, ICompressionService compressionService, FileTransferSettings fileTransferSettings)
         : base(crypterApiService, simpleEncryptionService, simpleSignatureService, userSessionService)
      {
         _compressionService = compressionService;
         _fileTransferSettings = fileTransferSettings;
      }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadPreviewAsync()
      {
#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousFilePreviewAsync(_transferId),
            TransferUserType.User => await _crypterApiService.DownloadUserFilePreviewAsync(_transferId, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524

         response.DoRight(x => {
            byte[] decodedPublicKey = Convert.FromBase64String(x.DiffieHellmanPublicKey);
            string pemFormattedKey = Encoding.UTF8.GetString(decodedPublicKey);
            SetSenderDiffieHellmanPublicKey(PEMString.From(pemFormattedKey));
         });
         return response;
      }

      public async Task<Either<DownloadTransferCiphertextError, byte[]>> DownloadCiphertextAsync(Maybe<Func<Task>> invokeBeforeDecryption, Maybe<Func<Task>> invokeBeforeVerification, Maybe<Func<Task>> invokeBeforeDecompression)
      {
         var request = _serverKey.Match(
            () => throw new Exception("Missing server key"),
            x => new DownloadTransferCiphertextRequest(x));

#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousFileCiphertextAsync(_transferId, request),
            TransferUserType.User => await _crypterApiService.DownloadUserFileCiphertextAsync(_transferId, request, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524
         return await response.MatchAsync<Either<DownloadTransferCiphertextError, byte[]>>(
            left => left,
            async right =>
            {
               string digitalSignaturePublicKeyPEM = Encoding.UTF8.GetString(
                  Convert.FromBase64String(right.DigitalSignaturePublicKey));

               SetSenderDigitalSignaturePublicKey(PEMString.From(digitalSignaturePublicKeyPEM));

               await invokeBeforeDecryption.IfSomeAsync(async x => await x.Invoke());

               byte[] plaintext = DecryptFile(right.Ciphertext, right.InitializationVector);
               await invokeBeforeVerification.IfSomeAsync(async x => await x.Invoke());

               bool verifiedFile = VerifyFile(plaintext, Convert.FromBase64String(right.DigitalSignature));

               if (verifiedFile)
               {
                  if (right.CompressionType == CompressionType.GZip)
                  {
                     await invokeBeforeDecompression.IfSomeAsync(async x => await x.Invoke());

                     using MemoryStream compressedStream = new MemoryStream(plaintext);
                     using MemoryStream decompressedPlaintext = await _compressionService.DecompressStreamAsync(compressedStream, compressedStream.Length, _fileTransferSettings.PartSizeBytes, Maybe<Func<double, Task>>.None);
                     return decompressedPlaintext.ToArray();
                  }
                  else
                  {
                     return plaintext;
                  }
               }

               return DownloadTransferCiphertextError.UnknownError;
            },
            DownloadTransferCiphertextError.UnknownError);
      }

      private byte[] DecryptFile(List<string> partionedCiphertext, string initializationVector)
      {
         byte[] ciphertext = partionedCiphertext
               .SelectMany(x => Convert.FromBase64String(x))
               .ToArray();

         byte[] iv = Convert.FromBase64String(initializationVector);

         return _symmetricKey.Match(
            () => throw new Exception("Missing symmetric key"),
            x => _simpleEncryptionService.Decrypt(x, iv, ciphertext));
      }

      private bool VerifyFile(byte[] file, byte[] signature)
      {
         PEMString verificationKey = _senderDigitalSignaturePublicKey.Match(
            () => throw new Exception("Missing digital signature public key"),
            x => x);

         return _simpleSignatureService.Verify(verificationKey, file, signature);
      }
   }
}
