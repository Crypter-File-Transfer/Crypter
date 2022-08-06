﻿/*
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
using Crypter.CryptoLib;
using Crypter.CryptoLib.Models;
using Crypter.CryptoLib.SodiumLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Transfer.Handlers
{
   public class UploadFileHandler : UploadHandler
   {
      private byte[] _fileBytes;
      private string _fileName;
      private long _fileSize;
      private string _fileContentType;
      private int _expirationHours;
      private bool _useCompression;
      private CompressionType _usedCompressionType;

      private readonly ICompressionService _compressionService;
      private readonly HashSet<string> _fileExtensionCompressionBlacklist;

      public readonly int BufferSize;

      public UploadFileHandler(ICrypterApiService crypterApiService, FileTransferSettings fileTransferSettings, ICompressionService compressionService)
         : base(crypterApiService, fileTransferSettings)
      {
         _compressionService = compressionService;
         BufferSize = fileTransferSettings.PartSizeBytes;

         _fileExtensionCompressionBlacklist = new HashSet<string>
         {
            "7z",
            "arj",
            "deb",
            "gz",
            "pkg",
            "rar",
            "rpm",
            "z",
            "zip"
         };

         _usedCompressionType = CompressionType.None;
      }

      internal void SetTransferInfo(byte[] fileBytes, string fileName, long fileSize, string fileContentType, int expirationHours, bool useCompression)
      {
         _fileBytes = fileBytes;
         _fileName = fileName;
         _fileSize = fileSize;
         _fileContentType = fileContentType;
         _expirationHours = expirationHours;
         _useCompression = useCompression;
      }

      public async Task<Either<UploadTransferError, UploadHandlerResponse>> UploadAsync(Maybe<Func<double, Task>> encryptionProgress, Maybe<Func<Task>> invokeBeforeUploading)
      {
         if (_recipientUsername.IsNone)
         {
            CreateEphemeralRecipientKeys();
         }

         if (!_senderDefined)
         {
            CreateEphemeralSenderKeys();
         }

         AsymmetricKeyPair senderKeyPair = _senderKeyPair.Match(
            () => throw new Exception("Missing sender private key"),
            x => x);

         byte[] recipientPublicKey = _recipientPublicKey.Match(
            () => throw new Exception("Missing recipient public key"),
            x => x);

         byte[] nonce = KDF.GenerateNonce();
         TransmissionKeyRing txKeyRing = KDF.CreateTransmissionKeys(senderKeyPair, recipientPublicKey, nonce);

         EncryptedBox box = SecretBox.Create(_fileBytes, txKeyRing.SendKey);
         await invokeBeforeUploading.IfSomeAsync(async x => await x.Invoke());

         var request = new UploadFileTransferRequest(_fileName, _fileContentType, box, txKeyRing.ServerProof, senderKeyPair.PublicKey, nonce, _expirationHours, _usedCompressionType);
         var response = await _recipientUsername.Match(
            () => _crypterApiService.UploadFileTransferAsync(request, _senderDefined),
            x => _crypterApiService.SendUserFileTransferAsync(x.Value, request, _senderDefined));
 
         return response.Map(x => new UploadHandlerResponse(x.Id, _expirationHours, TransferItemType.File, x.UserType, _recipientPrivateKey));
      }

      private bool UseCompressionOnFile()
      {
         string fileExtension = _fileName.Contains('.')
            ? _fileName.Split('.').Last()
            : string.Empty;

         return _useCompression && !_fileExtensionCompressionBlacklist.Contains(fileExtension);
      }
   }
}
