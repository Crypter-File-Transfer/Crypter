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
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Models;
using Crypter.CryptoLib.SodiumLib;
using System;

namespace Crypter.ClientServices.Transfer.Handlers.Base
{
   public class DownloadHandler : IUserDownloadHandler
   {
      protected readonly ICrypterApiService _crypterApiService;
      protected readonly IUserSessionService _userSessionService;

      protected Guid _transferId;
      protected TransferUserType _transferUserType = TransferUserType.Anonymous;

      protected Maybe<AsymmetricKeyPair> _recipientKeyPair = Maybe<AsymmetricKeyPair>.None;
      protected Maybe<byte[]> _senderPublicKey = Maybe<byte[]>.None;
      protected Maybe<byte[]> _nonce = Maybe<byte[]>.None;

      protected Maybe<TransmissionKeyRing> _txKeyRing = Maybe<TransmissionKeyRing>.None;

      public DownloadHandler(ICrypterApiService crypterApiService, IUserSessionService userSessionService)
      {
         _crypterApiService = crypterApiService;
         _userSessionService = userSessionService;
      }

      internal void SetTransferInfo(Guid id, TransferUserType userType)
      {
         _transferId = id;
         _transferUserType = userType;
      }

      public void SetRecipientInfo(byte[] recipientPrivateKey)
      {
         byte[] publicKey = ScalarMult.GetPublicKey(recipientPrivateKey);
         _recipientKeyPair = new AsymmetricKeyPair(recipientPrivateKey, publicKey);
         TryDeriveSymmetricKeys();
      }

      protected void SetKdfValuesFromApi(byte[] senderPublicKey, byte[] nonce)
      {
         _senderPublicKey = senderPublicKey;
         _nonce = nonce;
         TryDeriveSymmetricKeys();
      }

      private void TryDeriveSymmetricKeys()
      {
         _recipientKeyPair.IfSome(recipientKeyPair =>
         {
            _senderPublicKey.IfSome(senderKey =>
            {
               _nonce.IfSome(nonce =>
               {
                  _txKeyRing = KDF.CreateTransmissionKeys(recipientKeyPair, senderKey, nonce);
               });
            });
         });
      }
   }
}
