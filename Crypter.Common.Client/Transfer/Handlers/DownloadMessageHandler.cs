﻿/*
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
using System.Text;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Transfer.Handlers.Base;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.StreamEncryption;
using EasyMonads;

namespace Crypter.Common.Client.Transfer.Handlers;

public class DownloadMessageHandler : DownloadHandler
{
    public DownloadMessageHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider,
        IUserSessionService userSessionService, ClientTransferSettings clientTransferSettings)
        : base(crypterApiClient, cryptoProvider, userSessionService, clientTransferSettings)
    {
    }

    public async Task<Either<TransferPreviewError, MessageTransferPreviewResponse>> DownloadPreviewAsync()
    {
#pragma warning disable CS8524
        var response = TransferUserType switch
        {
            TransferUserType.Anonymous => await CrypterApiClient.MessageTransfer.GetAnonymousMessagePreviewAsync(
                TransferHashId!),
            TransferUserType.User => await CrypterApiClient.MessageTransfer.GetUserMessagePreviewAsync(TransferHashId!,
                UserSessionService.Session.IsSome)
        };
#pragma warning restore CS8524

        response.DoRight(x => SetSenderPublicKey(x.PublicKey, x.KeyExchangeNonce));
        return response;
    }

    public async Task<Either<DownloadTransferCiphertextError, string>> DownloadCiphertextAsync()
    {
        byte[] symmetricKey = SymmetricKey.Match(
            () => throw new Exception("Missing symmetric key"),
            x => x);

        byte[] serverProof = ServerProof.Match(
            () => throw new Exception("Missing server proof"),
            x => x);

#pragma warning disable CS8524
        Either<DownloadTransferCiphertextError, StreamDownloadResponse> response = TransferUserType switch
        {
            TransferUserType.Anonymous => await CrypterApiClient.MessageTransfer.GetAnonymousMessageCiphertextAsync(
                TransferHashId!, serverProof),
            TransferUserType.User => await CrypterApiClient.MessageTransfer.GetUserMessageCiphertextAsync(
                TransferHashId!, serverProof, UserSessionService.Session.IsSome)
        };
#pragma warning restore CS8524

        return await response.MatchAsync<Either<DownloadTransferCiphertextError, string>>(
            error => error,
            async streamDownloadResponse =>
            {
                await using DecryptionStream decryptionStream = await DecryptionStream.OpenAsync(streamDownloadResponse.Stream,
                    streamDownloadResponse.StreamSize, symmetricKey, CryptoProvider.StreamEncryptionFactory);
                
                byte[] plaintextBuffer = new byte[checked((int)streamDownloadResponse.StreamSize)];
                int plaintextPosition = 0;
                int bytesRead;
                do
                {
                    bytesRead = await decryptionStream.ReadAsync(plaintextBuffer.AsMemory(plaintextPosition));
                    plaintextPosition += bytesRead;
                } while (bytesRead > 0);
                
                return Encoding.UTF8.GetString(plaintextBuffer[..plaintextPosition]);
            },
            DownloadTransferCiphertextError.UnknownError);
    }
}
