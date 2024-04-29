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
    protected readonly ICrypterApiClient CrypterApiClient;
    protected readonly ICryptoProvider CryptoProvider;
    protected readonly ClientTransferSettings ClientTransferSettings;

    protected TransferUserType TransferUserType = TransferUserType.Anonymous;

    protected int ExpirationHours;

    protected bool SenderDefined;

    protected byte[] KeyExchangeNonce;
    protected Maybe<byte[]> SenderPrivateKey = Maybe<byte[]>.None;

    protected Maybe<string> RecipientUsername = Maybe<string>.None;
    protected Maybe<byte[]> RecipientKeySeed = Maybe<byte[]>.None;

    protected Maybe<byte[]> RecipientPrivateKey = Maybe<byte[]>.None;
    protected Maybe<byte[]> RecipientPublicKey = Maybe<byte[]>.None;

    protected UploadHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider,
        ClientTransferSettings clientTransferSettings)
    {
        CrypterApiClient = crypterApiClient;
        CryptoProvider = cryptoProvider;
        ClientTransferSettings = clientTransferSettings;

        KeyExchangeNonce = CryptoProvider.Random.GenerateRandomBytes((int)CryptoProvider.KeyExchange.NonceSize);
    }

    public void SetSenderInfo(byte[] privateKey)
    {
        SenderDefined = true;
        TransferUserType = TransferUserType.User;
        SenderPrivateKey = privateKey;
    }

    public void SetRecipientInfo(string username, byte[] publicKey)
    {
        TransferUserType = TransferUserType.User;
        RecipientUsername = username;
        RecipientPublicKey = publicKey;
    }

    protected void CreateEphemeralSenderKeys()
    {
        X25519KeyPair senderX25519KeyPair = CryptoProvider.KeyExchange.GenerateKeyPair();
        SenderPrivateKey = senderX25519KeyPair.PrivateKey;
    }

    protected void CreateEphemeralRecipientKeys()
    {
        Span<byte> seed = CryptoProvider.Random.GenerateRandomBytes((int)CryptoProvider.KeyExchange.SeedSize);
        RecipientKeySeed = seed.ToArray();
        X25519KeyPair recipientKeyPair = CryptoProvider.KeyExchange.GenerateKeyPairDeterministic(seed);
        RecipientPrivateKey = recipientKeyPair.PrivateKey;
        RecipientPublicKey = recipientKeyPair.PublicKey;
    }

    protected (Func<EncryptionStream> encryptionStreamOpener, byte[]? senderPublicKey, byte[] proof) GetEncryptionInfo(
        Func<Stream> plaintextStreamOpener, long streamSize)
    {
        if (RecipientUsername.IsNone)
        {
            CreateEphemeralRecipientKeys();
        }

        if (!SenderDefined)
        {
            CreateEphemeralSenderKeys();
        }

        byte[] senderPrivateKey = SenderPrivateKey.Match(
            () => throw new Exception("Missing sender private key"),
            x => x);

        byte[] recipientPublicKey = RecipientPublicKey.Match(
            () => throw new Exception("Missing recipient public key"),
            x => x);

        byte[] senderPublicKey = CryptoProvider.KeyExchange.GeneratePublicKey(senderPrivateKey);
        (byte[] encryptionKey, byte[] proof) = CryptoProvider.KeyExchange.GenerateEncryptionKey(
            CryptoProvider.StreamEncryptionFactory.KeySize, senderPrivateKey, recipientPublicKey, KeyExchangeNonce);

        byte[]? senderPublicKeyToUpload = SenderDefined
            ? null
            : senderPublicKey;

        return (EncryptionStreamOpener, senderPublicKeyToUpload, proof);

        EncryptionStream EncryptionStreamOpener()
            => new EncryptionStream(plaintextStreamOpener, streamSize, encryptionKey,
                CryptoProvider.StreamEncryptionFactory, ClientTransferSettings.MaxReadSize, ClientTransferSettings.PadSize);
    }
}
