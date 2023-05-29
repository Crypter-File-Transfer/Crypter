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
using System.Text;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Transfer.Handlers
{
   public class UploadMessageHandler : UploadHandler
   {
      private Func<MemoryStream> _messageStreamOpener;
      private string _messageSubject;
      private int _messageSize;

      public UploadMessageHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, TransferSettings transferSettings)
         : base(crypterApiClient, cryptoProvider, transferSettings)
      { }

      internal void SetTransferInfo(string messageSubject, string messageBody, int expirationHours)
      {
         _messageSubject = messageSubject;
         byte[] messageBytes = Encoding.UTF8.GetBytes(messageBody);
         _messageStreamOpener = () => new MemoryStream(messageBytes);
         _messageSize = messageBytes.Length;
         _expirationHours = expirationHours;
      }

      public Task<Either<UploadTransferError, UploadHandlerResponse>> UploadAsync()
      {
         var (encryptionStreamOpener, senderPublicKey, proof) = GetEncryptionInfo(_messageStreamOpener, _messageSize);
         UploadMessageTransferRequest request = new UploadMessageTransferRequest(_messageSubject, senderPublicKey, _keyExchangeNonce, proof, _expirationHours);
         return _crypterApiClient.MessageTransfer.UploadMessageTransferAsync(_recipientUsername, request, encryptionStreamOpener, _senderDefined)
            .MapAsync<UploadTransferError, UploadTransferResponse, UploadHandlerResponse>(x => new UploadHandlerResponse(x.HashId, _expirationHours, TransferItemType.Message, x.UserType, _recipientKeySeed));
      }
   }
}
