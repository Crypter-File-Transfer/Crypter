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

using System;
using System.IO;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Enums;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.KeyExchange;
using Crypter.Crypto.Common.StreamEncryption;
using EasyMonads;

namespace Crypter.Common.Client.Transfer.Handlers.Base;

public class UploadHandler : IUserUploadHandler
{
   protected readonly ICrypterApiClient _crypterApiClient;
   protected readonly ICryptoProvider _cryptoProvider;
   protected readonly TransferSettings _transferSettings;

   protected TransferUserType _transferUserType = TransferUserType.Anonymous;

   protected int _expirationHours;

   protected bool _senderDefined = false;

   protected byte[] _keyExchangeNonce;
   protected Maybe<byte[]> _senderPrivateKey = Maybe<byte[]>.None;

   protected Maybe<string> _recipientUsername = Maybe<string>.None;
   protected Maybe<byte[]> _recipientKeySeed = Maybe<byte[]>.None;

   protected Maybe<byte[]> _recipientPrivateKey = Maybe<byte[]>.None;
   protected Maybe<byte[]> _recipientPublicKey = Maybe<byte[]>.None;

   public UploadHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider, TransferSettings transferSettings)
   {
      _crypterApiClient = crypterApiClient;
      _cryptoProvider = cryptoProvider;
      _transferSettings = transferSettings;

      _keyExchangeNonce = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.KeyExchange.NonceSize);
   }

   public void SetSenderInfo(byte[] privateKey)
   {
      _senderDefined = true;
      _transferUserType = TransferUserType.User;
      _senderPrivateKey = privateKey;
   }

   public void SetRecipientInfo(string username, byte[] publicKey)
   {
      _transferUserType = TransferUserType.User;
      _recipientUsername = username;
      _recipientPublicKey = publicKey;
   }

   protected void CreateEphemeralSenderKeys()
   {
      X25519KeyPair senderX25519KeyPair = _cryptoProvider.KeyExchange.GenerateKeyPair();
      _senderPrivateKey = senderX25519KeyPair.PrivateKey;
   }

   protected void CreateEphemeralRecipientKeys()
   {
      Span<byte> seed = _cryptoProvider.Random.GenerateRandomBytes((int)_cryptoProvider.KeyExchange.SeedSize);
      _recipientKeySeed = seed.ToArray();
      X25519KeyPair recipientKeyPair = _cryptoProvider.KeyExchange.GenerateKeyPairDeterministic(seed);
      _recipientPrivateKey = recipientKeyPair.PrivateKey;
      _recipientPublicKey = recipientKeyPair.PublicKey;
   }

   protected (Func<EncryptionStream> encryptionStreamOpener, byte[] senderPublicKey, byte[] proof) GetEncryptionInfo(Func<Stream> plaintextStreamOpener, long streamSize)
   {
      if (_recipientUsername.IsNone)
      {
         CreateEphemeralRecipientKeys();
      }

      if (!_senderDefined)
      {
         CreateEphemeralSenderKeys();
      }

      byte[] senderPrivateKey = _senderPrivateKey.Match(
         () => throw new Exception("Missing sender private key"),
         x => x);

      byte[] recipientPublicKey = _recipientPublicKey.Match(
         () => throw new Exception("Missing recipient public key"),
         x => x);

      byte[] senderPublicKey = _cryptoProvider.KeyExchange.GeneratePublicKey(senderPrivateKey);
      (byte[] encryptionKey, byte[] proof) = _cryptoProvider.KeyExchange.GenerateEncryptionKey(_cryptoProvider.StreamEncryptionFactory.KeySize, senderPrivateKey, recipientPublicKey, _keyExchangeNonce);

      byte[] senderPublicKeyToUpload = _senderDefined
         ? null
         : senderPublicKey;

      EncryptionStream encryptionStreamOpener()
         => new EncryptionStream(plaintextStreamOpener(), streamSize, encryptionKey, _cryptoProvider.StreamEncryptionFactory, _transferSettings.MaxReadSize, _transferSettings.PadSize);

      return (encryptionStreamOpener, senderPublicKeyToUpload, proof);
   }
}