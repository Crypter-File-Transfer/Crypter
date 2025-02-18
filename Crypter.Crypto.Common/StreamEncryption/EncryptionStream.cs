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
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Crypto.Common.StreamEncryption;

public class EncryptionStream : Stream
{
    private const int LengthBufferSize = sizeof(int);

    private readonly IStreamEncrypt _streamEncrypt;
    private readonly Func<Stream> _plaintextStreamOpener;
    private Stream? _plaintextStream;
    private readonly long _plaintextSize;
    private readonly int _plaintextReadSize;
    private readonly byte[] _headerBytes;
    private readonly Action<double>? _updateCallback;
    
    private bool _finishedReadingPlaintext;
    private long _plaintextReadPosition;
    private bool _headerHasBeenReturned;
    
    public int MinimumBufferSize { get; init; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="plaintextStreamOpener"></param>
    /// <param name="plaintextSize"></param>
    /// <param name="encryptionKey"></param>
    /// <param name="streamEncryptionFactory"></param>
    /// <param name="maxReadSize"></param>
    /// <param name="padSize"></param>
    /// <param name="updateCallback"></param>
    public EncryptionStream(
        Func<Stream> plaintextStreamOpener,
        long plaintextSize,
        Span<byte> encryptionKey,
        IStreamEncryptionFactory streamEncryptionFactory,
        int maxReadSize,
        int padSize,
        Action<double>? updateCallback = null)
    {
        _plaintextStreamOpener = plaintextStreamOpener;
        _plaintextSize = plaintextSize;
        _plaintextReadSize = maxReadSize;
        _updateCallback = updateCallback;

        _streamEncrypt = streamEncryptionFactory.NewEncryptionStream(padSize);
        _headerBytes = _streamEncrypt.GenerateHeader(encryptionKey);
        MinimumBufferSize = LengthBufferSize + maxReadSize + padSize + (int)_streamEncrypt.TagSize;
    }
    
    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position
    {
        get { return _plaintextReadPosition; }
        set { throw new NotSupportedException(); }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        AssertBufferSize(buffer.Length);

        if (_finishedReadingPlaintext)
        {
            return 0;
        }
        
        if (!_headerHasBeenReturned)
        {
            return WriteHeaderBytes(buffer.AsSpan(offset, count));
        }
        
        Stream plaintextStream = GetPlaintextStream();
        
        byte[] plaintextBuffer = ArrayPool<byte>.Shared.Rent(_plaintextReadSize);
        int plaintextBytesRead = plaintextStream.Read(plaintextBuffer.AsSpan(0, _plaintextReadSize));
        _plaintextReadPosition += plaintextBytesRead;
        _finishedReadingPlaintext = _plaintextReadPosition == _plaintextSize;

        byte[] ciphertext = plaintextBytesRead < _plaintextReadSize
            ? _streamEncrypt.Push(plaintextBuffer[..plaintextBytesRead], _finishedReadingPlaintext)
            : _streamEncrypt.Push(plaintextBuffer[.._plaintextReadSize], _finishedReadingPlaintext);
        ArrayPool<byte>.Shared.Return(plaintextBuffer, clearArray: true);
        
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset, LengthBufferSize), ciphertext.Length);

        int bufferCiphertextStartingPosition = offset + LengthBufferSize;
        int bufferCiphertextLength = count - LengthBufferSize;
        ciphertext.CopyTo(buffer.AsMemory(bufferCiphertextStartingPosition, bufferCiphertextLength));
        
        _updateCallback?.Invoke(Convert.ToDouble(_plaintextReadPosition) / Convert.ToDouble(_plaintextSize));
        return ciphertext.Length + LengthBufferSize;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        AssertBufferSize(buffer.Length);

        if (_finishedReadingPlaintext)
        {
            return 0;
        }
        
        if (!_headerHasBeenReturned)
        {
            return WriteHeaderBytes(buffer.Span);
        }
        Stream plaintextStream = GetPlaintextStream();
        
        byte[] plaintextBuffer = ArrayPool<byte>.Shared.Rent(_plaintextReadSize);
        int plaintextBytesRead = await plaintextStream.ReadAsync(plaintextBuffer.AsMemory(0, _plaintextReadSize), cancellationToken);
        _plaintextReadPosition += plaintextBytesRead;
        _finishedReadingPlaintext = _plaintextReadPosition == _plaintextSize;

        byte[] ciphertext = plaintextBytesRead < _plaintextReadSize
            ? _streamEncrypt.Push(plaintextBuffer[..plaintextBytesRead], _finishedReadingPlaintext)
            : _streamEncrypt.Push(plaintextBuffer[.._plaintextReadSize], _finishedReadingPlaintext);
        ArrayPool<byte>.Shared.Return(plaintextBuffer, clearArray: true);
        
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Span[..LengthBufferSize], ciphertext.Length);
        ciphertext.CopyTo(buffer[LengthBufferSize..]);
        
        _updateCallback?.Invoke(Convert.ToDouble(_plaintextReadPosition) / Convert.ToDouble(_plaintextSize));
        return ciphertext.Length + LengthBufferSize;
    }

    private int WriteHeaderBytes(Span<byte> buffer)
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer[..LengthBufferSize], _headerBytes.Length);
        _headerBytes.CopyTo(buffer[LengthBufferSize..]);
        _headerHasBeenReturned = true;
        
        return _headerBytes.Length + LengthBufferSize;
    }
    
    private Stream GetPlaintextStream()
    {
        return _plaintextStream ??= _plaintextStreamOpener();
    }
    
    private void AssertBufferSize(int bufferSize)
    {
        if (bufferSize < MinimumBufferSize)
        {
            throw new ArgumentOutOfRangeException($"buffer size must be greater than or equal {MinimumBufferSize}");
        }
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override void Close()
    {
        _plaintextStream?.Close();
        base.Close();
    }
    
    protected override void Dispose(bool disposing)
    {
        _plaintextStream?.Dispose();
        base.Dispose(disposing);
    }
    
    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        if (_plaintextStream is not null)
        {
            await _plaintextStream.DisposeAsync(); 
        }
        await base.DisposeAsync();
    }
}
