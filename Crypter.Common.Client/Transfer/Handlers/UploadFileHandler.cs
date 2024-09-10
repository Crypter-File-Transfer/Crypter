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
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Transfer.Handlers.Base;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Common.Mappers;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.StreamEncryption;
using EasyMonads;

namespace Crypter.Common.Client.Transfer.Handlers;

public class UploadFileHandler : UploadHandler
{
    private Func<Stream>? _fileStreamOpener;
    private string? _fileName;
    private long _fileSize;
    private string? _fileContentType;
    private bool _transferInfoSet;

    public UploadFileHandler(ICrypterApiClient crypterApiClient, ICryptoProvider cryptoProvider,
        ClientTransferSettings clientTransferSettings)
        : base(crypterApiClient, cryptoProvider, clientTransferSettings)
    {
    }

    internal void SetTransferInfo(Func<Stream> fileStreamOpener, string fileName, long fileSize, string fileContentType, int expirationHours)
    {
        _fileStreamOpener = fileStreamOpener;
        _fileName = fileName;
        _fileSize = fileSize;
        _fileContentType = fileContentType;
        ExpirationHours = expirationHours;
        _transferInfoSet = true;
    }

    public Task<Either<UploadTransferError, UploadHandlerResponse>> UploadAsync(Action<double>? updateCallback = null)
    {
        if (!_transferInfoSet)
        {
            return Either<UploadTransferError, UploadHandlerResponse>
                .From(UploadTransferError.UnknownError)
                .AsTask();
        }

        (Func<Action<double>?, EncryptionStream> encryptionStreamOpener, byte[]? senderPublicKey, byte[] proof) = GetEncryptionInfo(_fileStreamOpener!, _fileSize);
        UploadFileTransferRequest request = new UploadFileTransferRequest(_fileName!, _fileContentType!, senderPublicKey,
            KeyExchangeNonce, proof, ExpirationHours);
        
        if (SenderDefined)
        {
            // Initialize
            return CrypterApiClient.FileTransfer
                // Initialize
                .InitializeMultipartFileTransferAsync(RecipientUsername, request)

                // Upload
                .BindAsync(async initializeResult =>
                {
                    Dictionary<int, Task<Either<UploadMultipartFileTransferError, Unit>>> indexedUploadResults = new Dictionary<int, Task<Either<UploadMultipartFileTransferError, Unit>>>();
                    EncryptionStream encryptionStream = encryptionStreamOpener(updateCallback);
                    IAsyncEnumerable<Func<MemoryStream>> asyncEnumerable = SplitEncryptionStreamAsync(encryptionStream);

                    ParallelOptions parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = ClientTransferSettings.MaximumMultipartParallelism
                    };
                    SemaphoreSlim uploadLock = new SemaphoreSlim(1);
                    bool fault = false;
                    int currentPosition = 0;
                    await Parallel.ForEachAsync(asyncEnumerable, parallelOptions, async (streamOpener, _) =>
                    {
                        if (!fault)
                        {
                            try
                            {
                                await uploadLock.WaitAsync(CancellationToken.None);
                                Task<Either<UploadMultipartFileTransferError, Unit>> uploadTask = CrypterApiClient.FileTransfer
                                    .UploadMultipartFileTransferAsync(initializeResult.HashId, currentPosition, streamOpener)
                                    .ContinueWith(x =>
                                    {
                                        if (!x.Result.IsRight)
                                        {
                                            fault = true;
                                        }

                                        return x.Result;
                                    }, CancellationToken.None);
                                indexedUploadResults.Add(currentPosition, uploadTask);
                                currentPosition++;
                                uploadLock.Release();
                                await uploadTask;
                            }
                            catch (Exception)
                            {
                                fault = true;
                                throw;
                            }
                        }
                    });

                    Either<UploadMultipartFileTransferError, Unit> errorOrSuccess = indexedUploadResults
                        .OrderBy(x => x.Key)
                        .Select(x => x.Value.Result)
                        .FirstOrDefault(x => x.IsLeft || x.IsNeither, Either<UploadMultipartFileTransferError, Unit>.FromRight(Unit.Default));
                    
                    return await errorOrSuccess
                        .MapLeft(error => error.ConvertToUploadTransferError())
                        .BindAsync(async _ => await CrypterApiClient.FileTransfer.FinalizeMultipartFileTransferAsync(initializeResult.HashId)
                            .MapLeftAsync<FinalizeMultipartFileTransferError, Unit, UploadTransferError>(error => error.ConvertToUploadTransferError()))
                        .MapAsync<UploadTransferError, Unit, UploadHandlerResponse>(_ => new UploadHandlerResponse(initializeResult.HashId, ExpirationHours, TransferItemType.File, initializeResult.TransferUserType, RecipientKeySeed))
                        .DoLeftOrNeitherAsync(
                            leftAsync: async _ => await CrypterApiClient.FileTransfer.AbandonMultipartFileTransferAsync(initializeResult.HashId),
                            neitherAsync: async () => await CrypterApiClient.FileTransfer.AbandonMultipartFileTransferAsync(initializeResult.HashId));
                });
        }
        else
        {
            return CrypterApiClient.FileTransfer
                .UploadFileTransferAsync(RecipientUsername, request, encryptionStreamOpener, SenderDefined, updateCallback)
                .MapAsync<UploadTransferError, UploadTransferResponse, UploadHandlerResponse>(x =>
                    new UploadHandlerResponse(x.HashId, ExpirationHours, TransferItemType.File, x.UserType,
                        RecipientKeySeed));
        }
        
        async IAsyncEnumerable<Func<MemoryStream>> SplitEncryptionStreamAsync(EncryptionStream encryptionStream)
        {
            bool endOfStream = false;
            do
            {
                int bufferSize = ClientTransferSettings.MaximumMultipartReadBlocks * encryptionStream.MinimumBufferSize;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

                int totalBytesRead = 0;
                for (int i = 0; i < ClientTransferSettings.MaximumMultipartReadBlocks; i++)
                {
                    int bytesRead = await encryptionStream.ReadAsync(buffer.AsMemory(totalBytesRead, encryptionStream.MinimumBufferSize));
                    totalBytesRead += bytesRead;
                    if (bytesRead == 0)
                    {
                        endOfStream = true;
                        break;
                    }
                }
                
                if (totalBytesRead > 0)
                {
                    try
                    {
                        yield return () => new MemoryStream(buffer, 0, totalBytesRead);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);   
                    }
                }
            } while (!endOfStream);
        }
    }
}
