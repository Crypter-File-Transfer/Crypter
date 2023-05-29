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
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.StreamEncryption;
using System;
using System.IO;

namespace Crypter.Common.Client.Transfer.Handlers.Base
{
   public class DownloadHandler : IUserDownloadHandler
   {
      protected readonly ICrypterApiClient _crypterApiClient;
      protected readonly ICryptoProvider _cryptoProvider;
      protected readonly IUserSessionService _userSessionService;
      protected readonly TransferSettings _transferSettings;

      protected string _transferHashId;
      protected TransferUserType _transferUserType = TransferUserType.Anonymous;

      protected Maybe<byte[]> _recipientPrivateKey = Maybe<byte[]>.None;
      protected Maybe<byte[]> _senderPublicKey = Maybe<byte[]>.None;

      protected Maybe<byte[]> _symmetricKey = Maybe<byte[]>.None;
      protected Maybe<byte[]> _serverProof = Maybe<byte[]>.None;
      protected Maybe<byte[]> _keyExchangeNonce = Maybe<byte[]>.None;

      public DownloadHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, IUserSessionService userSessionService, TransferSettings transferSettings)
      {
         _crypterApiClient = crypterApiClient;
         _cryptoProvider = cryptoProvider;
         _userSessionService = userSessionService;
         _transferSettings = transferSettings;
      }

      internal void SetTransferInfo(string transferHashId, TransferUserType userType)
      {
         _transferHashId = transferHashId;
         _transferUserType = userType;
      }

      public void SetRecipientInfo(byte[] recipientPrivateKey)
      {
         _recipientPrivateKey = recipientPrivateKey;
         TryDeriveSymmetricKeys();
      }

      protected void SetSenderPublicKey(byte[] senderPublicKey, byte[] keyExchangeNonce)
      {
         _senderPublicKey = senderPublicKey;
         _keyExchangeNonce = keyExchangeNonce;
         TryDeriveSymmetricKeys();
      }

      private void TryDeriveSymmetricKeys()
      {
         _recipientPrivateKey.IfSome(privateKey =>
         {
            _senderPublicKey.IfSome(publicKey =>
            {
               _keyExchangeNonce.IfSome(keyExchangeNonce =>
               {
                  uint keySize = _cryptoProvider.StreamEncryptionFactory.KeySize;
                  (_symmetricKey, _serverProof) = _cryptoProvider.KeyExchange.GenerateDecryptionKey(keySize, privateKey, publicKey, keyExchangeNonce);
               });
            });
         });
      }

      protected byte[] Decrypt(byte[] key, Stream ciphertext, long streamSize)
      {
         using DecryptionStream decryptionStream = new DecryptionStream(ciphertext, streamSize, key, _cryptoProvider.StreamEncryptionFactory);
         byte[] plaintextBuffer = new byte[checked((int)streamSize)];
         int plaintextPosition = 0;
         int bytesRead;
         do
         {
            bytesRead = decryptionStream.Read(plaintextBuffer.AsSpan(plaintextPosition));
            plaintextPosition += bytesRead;
         }
         while (bytesRead > 0);
         return plaintextBuffer[..plaintextPosition];
      }
   }
}
