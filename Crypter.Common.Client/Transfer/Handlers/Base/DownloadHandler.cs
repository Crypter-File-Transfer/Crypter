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
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Enums;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.StreamEncryption;
using EasyMonads;

namespace Crypter.Common.Client.Transfer.Handlers.Base;

public class DownloadHandler : IUserDownloadHandler
{
    protected readonly ICrypterApiClient CrypterApiClient;
    protected readonly ICryptoProvider CryptoProvider;
    protected readonly IUserSessionService UserSessionService;
    protected readonly ClientTransferSettings ClientTransferSettings;

    protected string? TransferHashId;
    protected TransferUserType TransferUserType = TransferUserType.Anonymous;

    protected Maybe<byte[]> RecipientPrivateKey = Maybe<byte[]>.None;
    protected Maybe<byte[]> SenderPublicKey = Maybe<byte[]>.None;

    protected Maybe<byte[]> SymmetricKey = Maybe<byte[]>.None;
    protected Maybe<byte[]> ServerProof = Maybe<byte[]>.None;
    protected Maybe<byte[]> KeyExchangeNonce = Maybe<byte[]>.None;

    protected DownloadHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider,
        IUserSessionService userSessionService, ClientTransferSettings clientTransferSettings)
    {
        CrypterApiClient = crypterApiClient;
        CryptoProvider = cryptoProvider;
        UserSessionService = userSessionService;
        ClientTransferSettings = clientTransferSettings;
    }

    internal void SetTransferInfo(string transferHashId, TransferUserType userType)
    {
        TransferHashId = transferHashId;
        TransferUserType = userType;
    }

    public void SetRecipientInfo(byte[] recipientPrivateKey)
    {
        RecipientPrivateKey = recipientPrivateKey;
        TryDeriveSymmetricKeys();
    }

    protected void SetSenderPublicKey(byte[] senderPublicKey, byte[] keyExchangeNonce)
    {
        SenderPublicKey = senderPublicKey;
        KeyExchangeNonce = keyExchangeNonce;
        TryDeriveSymmetricKeys();
    }

    private void TryDeriveSymmetricKeys()
    {
        RecipientPrivateKey.IfSome(privateKey =>
        {
            SenderPublicKey.IfSome(publicKey =>
            {
                KeyExchangeNonce.IfSome(keyExchangeNonce =>
                {
                    uint keySize = CryptoProvider.StreamEncryptionFactory.KeySize;
                    (SymmetricKey, ServerProof) =
                        CryptoProvider.KeyExchange.GenerateDecryptionKey(keySize, privateKey, publicKey,
                            keyExchangeNonce);
                });
            });
        });
    }

    protected byte[] Decrypt(byte[] key, Stream ciphertext, long streamSize)
    {
        using DecryptionStream decryptionStream =
            new DecryptionStream(ciphertext, streamSize, key, CryptoProvider.StreamEncryptionFactory);
        byte[] plaintextBuffer = new byte[checked((int)streamSize)];
        int plaintextPosition = 0;
        int bytesRead;
        do
        {
            bytesRead = decryptionStream.Read(plaintextBuffer.AsSpan(plaintextPosition));
            plaintextPosition += bytesRead;
        } while (bytesRead > 0);

        return plaintextBuffer[..plaintextPosition];
    }
}
