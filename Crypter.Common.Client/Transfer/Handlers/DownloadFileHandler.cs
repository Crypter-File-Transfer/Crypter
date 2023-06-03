/*
 * Copyright (C) 2023 Crypter File Transfer
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

using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Transfer.Handlers.Base;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Crypto.Common;
using System;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Transfer.Handlers
{
   public class DownloadFileHandler : DownloadHandler
   {
      public DownloadFileHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserSessionService userSessionService, TransferSettings transferSettings)
         : base(crypterApiClient, cryptoProvider, userSessionService, transferSettings)
      { }

      public async Task<Either<TransferPreviewError, FileTransferPreviewResponse>> DownloadPreviewAsync()
      {
#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiClient.FileTransfer.GetAnonymousFilePreviewAsync(_transferHashId),
            TransferUserType.User => await _crypterApiClient.FileTransfer.GetUserFilePreviewAsync(_transferHashId, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524

         response.DoRight(x => SetSenderPublicKey(x.PublicKey, x.KeyExchangeNonce));
         return response;
      }

      public async Task<Either<DownloadTransferCiphertextError, byte[]>> DownloadCiphertextAsync()
      {
         byte[] symmetricKey = _symmetricKey.Match(
            () => throw new Exception("Missing symmetric key"),
            x => x);

         byte[] serverProof = _serverProof.Match(
            () => throw new Exception("Missing server proof"),
            x => x);

#pragma warning disable CS8524
         Either<DownloadTransferCiphertextError, StreamDownloadResponse> response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiClient.FileTransfer.GetAnonymousFileCiphertextAsync(_transferHashId, serverProof),
            TransferUserType.User => await _crypterApiClient.FileTransfer.GetUserFileCiphertextAsync(_transferHashId, serverProof, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524

         return response.Match<Either<DownloadTransferCiphertextError, byte[]>>(
            left => left,
            right => Decrypt(symmetricKey, right.Stream, right.StreamSize),
            DownloadTransferCiphertextError.UnknownError);
      }
   }
}
