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
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.StreamEncryption;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class DownloadFileHandler : DownloadHandler
   {
      public DownloadFileHandler(ICrypterApiService crypterApiService, ICryptoProvider cryptoProvider, IUserSessionService userSessionService, TransferSettings transferSettings)
         : base(crypterApiService, cryptoProvider, userSessionService, transferSettings)
      { }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferFilePreviewResponse>> DownloadPreviewAsync()
      {
#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousFilePreviewAsync(_transferHashId),
            TransferUserType.User => await _crypterApiService.DownloadUserFilePreviewAsync(_transferHashId, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524

         response.DoRight(x => SetSenderPublicKey(x.PublicKey, x.KeyExchangeNonce));
         return response;
      }

      public async Task<Either<DownloadTransferCiphertextError, byte[]>> DownloadCiphertextAsync(Maybe<Func<Task>> invokeBeforeDecryption)
      {
         byte[] symmetricKey = _symmetricKey.Match(
            () => throw new Exception("missing symmetric key"),
            x => x);

         DownloadTransferCiphertextRequest request = _serverProof.Match(
            () => throw new Exception("Missing server key"),
            x => new DownloadTransferCiphertextRequest(x));

#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousFileCiphertextAsync(_transferHashId, request),
            TransferUserType.User => await _crypterApiService.DownloadUserFileCiphertextAsync(_transferHashId, request, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524
         return await response.MatchAsync<Either<DownloadTransferCiphertextError, byte[]>>(
            left => left,
            async right =>
            {
               await invokeBeforeDecryption.IfSomeAsync(async x => await x.Invoke());
               return DecryptFile(symmetricKey, right.Header, right.Ciphertext);
            },
            DownloadTransferCiphertextError.UnknownError);
      }

      private byte[] DecryptFile(byte[] key, byte[] header, List<byte[]> partionedCiphertext)
      {
         List<byte[]> plaintextChunks = new List<byte[]>(partionedCiphertext.Count);
         IStreamDecrypt decryptionStream = _cryptoProvider.StreamEncryptionFactory.NewDecryptionStream(key, header, _transferSettings.PaddingBlockSize);
         for (int i = 0; i < partionedCiphertext.Count; i++)
         {
            plaintextChunks.Add(decryptionStream.Pull(partionedCiphertext[i], out bool final));
            if (final && i != partionedCiphertext.Count -1)
            {
               throw new CryptographicException("Unexpected 'final' chunk.");
            }
            else if (i == partionedCiphertext.Count - 1 && !final)
            {
               throw new CryptographicException("Missing 'final' chunk.");
            }
         }

         int plaintextSize = plaintextChunks.Sum(x => x.Length);
         byte[] plaintextWhole = new byte[plaintextSize];
         int plaintextPosition = 0;
         foreach (byte[] plaintextChunk in plaintextChunks)
         {
            plaintextChunk.CopyTo(plaintextWhole, plaintextPosition);
            plaintextPosition += plaintextChunk.Length;
         }
         return plaintextWhole;
      }
   }
}
