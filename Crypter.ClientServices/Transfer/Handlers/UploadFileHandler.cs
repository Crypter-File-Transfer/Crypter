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
using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class UploadFileHandler : UploadHandler
   {
      private Stream _encryptionStream;
      private Stream _signingStream;
      private string _fileName;
      private long _fileSize;
      private string _fileContentType;
      private int _expirationHours;

      public UploadFileHandler(ICrypterApiService crypterApiService, ISimpleEncryptionService simpleEncryptionService, ISimpleSignatureService simpleSignatureService, UploadSettings uploadSettings)
         : base(crypterApiService, simpleEncryptionService, simpleSignatureService, uploadSettings)
      { }

      internal void SetTransferInfo(Stream fileStream, Stream signingStream, string fileName, long fileSize, string fileContentType, int expirationHours)
      {
         _encryptionStream = fileStream;
         _signingStream = signingStream;
         _fileName = fileName;
         _fileSize = fileSize;
         _fileContentType = fileContentType;
         _expirationHours = expirationHours;
      }

      public async Task<Either<UploadTransferError, UploadHandlerResponse>> UploadAsync(Maybe<Func<double, Task>> encryptionProgress, Maybe<Func<double, Task>> signatureProgress, Maybe<Func<Task>> invokeBeforeUploading)
      {
         if (_recipientUsername.IsNone)
         {
            CreateEphemeralRecipientKeys();
         }

         if (!_senderDefined)
         {
            CreateEphemeralSenderKeys();
         }

         PEMString senderDiffieHellmanPrivateKey = _senderDiffieHellmanPrivateKey.Match(
            () => throw new Exception("Missing sender Diffie Hellman private key"),
            x => x);

         PEMString recipientDiffieHellmanPublicKey = _recipientDiffieHellmanPublicKey.Match(
            () => throw new Exception("Missing recipient Diffie Hellman private key"),
            x => x);

         PEMString senderDigitalSignaturePrivateKey = _senderDigitalSignaturePrivateKey.Match(
            () => throw new Exception("Missing recipient Digital Signature private key"),
            x => x);

         (byte[] sendKey, byte[] serverKey) = DeriveSymmetricKeys(senderDiffieHellmanPrivateKey, recipientDiffieHellmanPublicKey);
         byte[] initializationVector = AES.GenerateIV();

         List<byte[]> partitionedCiphertext = await _simpleEncryptionService.EncryptStreamAsync(sendKey, initializationVector, _encryptionStream, _fileSize, _uploadSettings.PartSizeBytes, encryptionProgress);
         byte[] signature = await _simpleSignatureService.SignStreamAsync(senderDigitalSignaturePrivateKey, _signingStream, _fileSize, _uploadSettings.PartSizeBytes, signatureProgress);
         await invokeBeforeUploading.IfSomeAsync(async x => await x.Invoke());

         string encodedInitializationVector = Convert.ToBase64String(initializationVector);

         List<string> encodedCipherText = partitionedCiphertext
            .Select(x => Convert.ToBase64String(x))
            .ToList();

         string encodedSignature = Convert.ToBase64String(signature);

         string encodedECDSASenderKey = _senderDigitalSignaturePublicKey.Match(
            () => throw new Exception("Missing sender Digital Signature public key"),
            x =>
            {
               return Convert.ToBase64String(
                  Encoding.UTF8.GetBytes(x.Value));
            });

         string encodedECDHSenderKey = _senderDiffieHellmanPublicKey.Match(
            () => throw new Exception("Missing sender Diffie Hellman public key"),
            x =>
            {
               return Convert.ToBase64String(
                  Encoding.UTF8.GetBytes(x.Value));
            });

         string encodedServerKey = Convert.ToBase64String(serverKey);

         var request = new UploadFileTransferRequest(_fileName, _fileContentType, encodedInitializationVector, encodedCipherText, encodedSignature, encodedECDSASenderKey, encodedECDHSenderKey, encodedServerKey, _expirationHours);
         var response = await _recipientUsername.Match(
            () => _crypterApiService.UploadFileTransferAsync(request, _senderDefined),
            x => _crypterApiService.SendUserFileTransferAsync(x, request, _senderDefined));
 
         return response.Map(x => new UploadHandlerResponse(x.Id, _expirationHours, TransferItemType.File, x.UserType, _recipientDiffieHellmanPrivateKey));
      }
   }
}
