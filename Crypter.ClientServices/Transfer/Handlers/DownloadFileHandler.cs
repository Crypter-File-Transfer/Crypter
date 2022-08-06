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
using Crypter.Contracts.Features.Transfer;
using Crypter.CryptoLib.Models;
using Crypter.CryptoLib.SodiumLib;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class DownloadFileHandler : DownloadHandler
   {
      private readonly ICompressionService _compressionService;
      private readonly FileTransferSettings _fileTransferSettings;

      public DownloadFileHandler(ICrypterApiService crypterApiService, IUserSessionService userSessionService, ICompressionService compressionService, FileTransferSettings fileTransferSettings)
         : base(crypterApiService, userSessionService)
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

         response.DoRight(x => SetKdfValuesFromApi(x.PublicKey, x.Nonce));
         return response;
      }

      public async Task<Either<DownloadTransferCiphertextError, byte[]>> DownloadCiphertextAsync(Maybe<Func<Task>> invokeBeforeDecryption, Maybe<Func<Task>> invokeBeforeDecompression)
      {
         var request = _txKeyRing.Match(
            () => throw new Exception("Missing key ring"),
            x => new DownloadTransferCiphertextRequest(x.ServerProof));

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
               await invokeBeforeDecryption.IfSomeAsync(async x => await x.Invoke());

               TransmissionKeyRing keyRing = _txKeyRing.Match(
                  () => throw new Exception("Missing key ring"),
                  x => x);

               byte[] plaintext = SecretBox.Open(right.Box, keyRing.ReceiveKey);

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
            },
            DownloadTransferCiphertextError.UnknownError);
      }
   }
}
