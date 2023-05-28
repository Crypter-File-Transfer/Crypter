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

using Crypter.Common.Client.Interfaces;
using Crypter.Common.Client.Transfer.Handlers.Base;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Crypto.Common;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Transfer.Handlers
{
   public class UploadFileHandler : UploadHandler
   {
      private Func<Stream> _fileStreamOpener;
      private string _fileName;
      private long _fileSize;
      private string _fileContentType;

      public UploadFileHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, TransferSettings transferSettings)
         : base(crypterApiClient, cryptoProvider, transferSettings)
      { }

      internal void SetTransferInfo(Func<Stream> fileStreamOpener, string fileName, long fileSize, string fileContentType, int expirationHours)
      {
         _fileStreamOpener = fileStreamOpener;
         _fileName = fileName;
         _fileSize = fileSize;
         _fileContentType = fileContentType;
         _expirationHours = expirationHours;
      }

      public Task<Either<UploadTransferError, UploadHandlerResponse>> UploadAsync()
      {
         var (encryptionStreamOpener, senderPublicKey, proof) = GetEncryptionInfo(_fileStreamOpener, _fileSize);
         UploadFileTransferRequest request = new UploadFileTransferRequest(_fileName, _fileContentType, senderPublicKey, _keyExchangeNonce, proof, _expirationHours);
         return _crypterApiClient.FileTransfer.UploadFileTransferAsync(_recipientUsername, request, encryptionStreamOpener, _senderDefined)
            .MapAsync<UploadTransferError, UploadTransferResponse, UploadHandlerResponse>(x => new UploadHandlerResponse(x.HashId, _expirationHours, TransferItemType.File, x.UserType, _recipientKeySeed));
      }
   }
}
