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
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class DownloadMessageHandler : DownloadHandler
   {
      public DownloadMessageHandler(ICrypterApiService crypterApiService, IUserSessionService userSessionService)
         : base(crypterApiService, userSessionService)
      { }

      public async Task<Either<DownloadTransferPreviewError, DownloadTransferMessagePreviewResponse>> DownloadPreviewAsync()
      {
#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousMessagePreviewAsync(_transferId),
            TransferUserType.User => await _crypterApiService.DownloadUserMessagePreviewAsync(_transferId, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524

         response.DoRight(x => {
            byte[] decodedPublicKey = Convert.FromBase64String(x.PublicKey);
            string pemFormattedKey = Encoding.UTF8.GetString(decodedPublicKey);
            SetSenderPublicKey(PEMString.From(pemFormattedKey));
         });
         return response;
      }

      public async Task<Either<DownloadTransferCiphertextError, string>> DownloadCiphertextAsync(Maybe<Func<Task>> invokeAfterDownloading)
      {
         var request = _serverKey.Match(
            () => throw new Exception("Missing server key"),
            x => new DownloadTransferCiphertextRequest(x));

#pragma warning disable CS8524
         var response = _transferUserType switch
         {
            TransferUserType.Anonymous => await _crypterApiService.DownloadAnonymousMessageCiphertextAsync(_transferId, request),
            TransferUserType.User => await _crypterApiService.DownloadUserMessageCiphertextAsync(_transferId, request, _userSessionService.Session.IsSome)
         };
#pragma warning restore CS8524
         await invokeAfterDownloading.IfSomeAsync(async x => await x.Invoke());

         return response.Match<Either<DownloadTransferCiphertextError, string>>(
            left => left,
            right => DecryptMessage(right.Ciphertext, right.InitializationVector),
            DownloadTransferCiphertextError.UnknownError);
      }

      private string DecryptMessage(List<string> partionedCiphertext, string initializationVector)
      {
         byte[] ciphertext = partionedCiphertext
               .SelectMany(x => Convert.FromBase64String(x))
               .ToArray();

         byte[] iv = Convert.FromBase64String(initializationVector);

         return _symmetricKey.Match(
            () => throw new Exception("Missing symmetric key"),
            x => _simpleEncryptionService.DecryptToString(x, iv, ciphertext));
      }
   }
}
