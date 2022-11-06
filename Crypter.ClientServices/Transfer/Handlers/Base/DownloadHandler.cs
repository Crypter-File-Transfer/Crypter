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
using Crypter.Common.Primitives;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Services;
using Org.BouncyCastle.Crypto;
using System;

namespace Crypter.ClientServices.Transfer.Handlers.Base
{
   public class DownloadHandler : IUserDownloadHandler
   {
      protected readonly ICrypterApiService _crypterApiService;
      protected readonly ISimpleEncryptionService _simpleEncryptionService;
      protected readonly IUserSessionService _userSessionService;

      protected string _transferHashId;
      protected TransferUserType _transferUserType = TransferUserType.Anonymous;

      protected Maybe<PEMString> _recipientDiffieHellmanPrivateKey = Maybe<PEMString>.None;

      protected Maybe<PEMString> _senderDiffieHellmanPublicKey = Maybe<PEMString>.None;

      protected Maybe<byte[]> _symmetricKey = Maybe<byte[]>.None;
      protected Maybe<string> _serverKey = Maybe<string>.None;

      public DownloadHandler(ICrypterApiService crypterApiService, ISimpleEncryptionService simpleEncryptionService, IUserSessionService userSessionService)
      {
         _crypterApiService = crypterApiService;
         _simpleEncryptionService = simpleEncryptionService;
         _userSessionService = userSessionService;
      }

      internal void SetTransferInfo(string transferHashId, TransferUserType userType)
      {
         _transferHashId = transferHashId;
         _transferUserType = userType;
      }

      public void SetRecipientInfo(PEMString recipientDiffieHellmanPrivateKey)
      {
         _recipientDiffieHellmanPrivateKey = recipientDiffieHellmanPrivateKey;
         TryDeriveSymmetricKeys();
      }

      protected void SetSenderDiffieHellmanPublicKey(PEMString senderDiffieHellmanPublicKey)
      {
         _senderDiffieHellmanPublicKey = senderDiffieHellmanPublicKey;
         TryDeriveSymmetricKeys();
      }

      private void TryDeriveSymmetricKeys()
      {
         _recipientDiffieHellmanPrivateKey.IfSome(recipientKey =>
         {
            _senderDiffieHellmanPublicKey.IfSome(senderKey =>
            {
               (_symmetricKey, byte[] serverKey) = DeriveSymmetricKeys(recipientKey, senderKey);
               _serverKey = Convert.ToBase64String(serverKey);
            });
         });
      }

      protected static (byte[] ReceiveKey, byte[] ServerKey) DeriveSymmetricKeys(PEMString recipientX25519PrivateKey, PEMString senderX25519PublicKey)
      {
         var recipientX25519Private = KeyConversion.ConvertX25519PrivateKeyFromPEM(recipientX25519PrivateKey);
         var recipientX25519Public = recipientX25519Private.GeneratePublicKey();
         var recipientKeyPair = new AsymmetricCipherKeyPair(recipientX25519Public, recipientX25519Private);

         var senderX25519Public = KeyConversion.ConvertX25519PublicKeyFromPEM(senderX25519PublicKey);
         (var receiveKey, var sendKey) = ECDH.DeriveSharedKeys(recipientKeyPair, senderX25519Public);
         var serverKey = ECDH.DeriveKeyFromECDHDerivedKeys(receiveKey, sendKey);

         return (receiveKey, serverKey);
      }
   }
}
