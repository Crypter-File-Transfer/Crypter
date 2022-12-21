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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class UploadMessageHandler : UploadHandler
   {
      private MemoryStream _messageStream;
      private string _messageSubject;

      public UploadMessageHandler(ICrypterApiService crypterApiService, ICryptoProvider cryptoProvider, TransferSettings transferSettings)
         : base(crypterApiService, cryptoProvider, transferSettings)
      { }

      internal void SetTransferInfo(string messageSubject, string messageBody, int expirationHours)
      {
         _messageSubject = messageSubject;
         byte[] messageBytes = Encoding.UTF8.GetBytes(messageBody);
         _messageStream = new MemoryStream(messageBytes);
         _expirationHours = expirationHours;
      }

      public async Task<Either<UploadTransferError, UploadHandlerResponse>> UploadAsync()
      {
         var (encryptionStream, senderPublicKey, proof) = GetEncryptionInfo(_messageStream, _messageStream.Length);
         UploadMessageTransferRequest request = new UploadMessageTransferRequest(_messageSubject, senderPublicKey, _keyExchangeNonce, proof, _expirationHours);
         Either<UploadTransferError, UploadTransferResponse> response = await _recipientUsername.Match(
            () => _crypterApiService.UploadMessageTransferAsync(request, encryptionStream, _senderDefined),
            x => _crypterApiService.SendUserMessageTransferAsync(x, request, encryptionStream, _senderDefined));

         return response.Map(x => new UploadHandlerResponse(x.HashId, _expirationHours, TransferItemType.Message, x.UserType, _recipientKeySeed));
      }
   }
}
