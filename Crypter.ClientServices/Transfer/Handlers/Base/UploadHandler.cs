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
using Crypter.ClientServices.Transfer.Models;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.CryptoLib.Models;
using Crypter.CryptoLib.SodiumLib;

namespace Crypter.ClientServices.Transfer.Handlers.Base
{
   public class UploadHandler : IUserUploadHandler
   {
      protected readonly ICrypterApiService _crypterApiService;
      protected readonly FileTransferSettings _fileTransferSettings;

      protected TransferUserType _transferUserType = TransferUserType.Anonymous;

      protected bool _senderDefined = false;

      protected Maybe<byte[]> _senderPrivateKey = Maybe<byte[]>.None;
      protected Maybe<byte[]> _senderPublicKey = Maybe<byte[]>.None;

      protected Maybe<Username> _recipientUsername = Maybe<Username>.None;

      protected Maybe<byte[]> _recipientPrivateKey = Maybe<byte[]>.None;
      protected Maybe<byte[]> _recipientPublicKey = Maybe<byte[]>.None;

      public UploadHandler(ICrypterApiService crypterApiService, FileTransferSettings fileTransferSettings)
      {
         _crypterApiService = crypterApiService;
         _fileTransferSettings = fileTransferSettings;
      }

      public void SetSenderInfo(byte[] privateKey)
      {
         _senderDefined = true;
         _transferUserType = TransferUserType.User;

         _senderPrivateKey = privateKey;
         _senderPublicKey = ScalarMult.GetPublicKey(privateKey);
      }

      public void SetRecipientInfo(Username username, byte[] publicKey)
      {
         _transferUserType = TransferUserType.User;
         _recipientUsername = username;
         _recipientPublicKey = publicKey;
      }

      protected void CreateEphemeralSenderKeys()
      {
         AsymmetricKeyPair senderKeyPair = PublicKeyAuth.GenerateKeyPair();
         _senderPrivateKey = senderKeyPair.PrivateKey;
         _senderPublicKey = senderKeyPair.PublicKey;
      }

      protected void CreateEphemeralRecipientKeys()
      {
         AsymmetricKeyPair recipientKeyPair = PublicKeyAuth.GenerateKeyPair();
         _recipientPrivateKey = recipientKeyPair.PrivateKey;
         _recipientPublicKey = recipientKeyPair.PublicKey;
      }
   }
}
