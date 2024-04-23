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
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Crypto.Common.StreamEncryption;

public class EncryptionStream : Stream
{
    private const int LengthBufferSize = sizeof(int);

    private readonly IStreamEncrypt _streamEncrypt;
    private readonly Stream _plaintextStream;
    private readonly long _plaintextSize;
    private readonly int _plaintextReadSize;
    private readonly int _minimumBufferSize;
    private readonly byte[] _headerBytes;

    private bool _finishedReadingPlaintext;
    private long _plaintextReadPosition;
    private bool _headerHasBeenReturned;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="plaintextStream"></param>
    /// <param name="plaintextSize"></param>
    /// <param name="encryptionKey"></param>
    /// <param name="streamEncryptionFactory"></param>
    /// <param name="maxReadSize"></param>
    /// <param name="padSize"></param>
    public EncryptionStream(Stream plaintextStream, long plaintextSize, Span<byte> encryptionKey,
        IStreamEncryptionFactory streamEncryptionFactory, int maxReadSize, int padSize)
    {
        _plaintextStream = plaintextStream;
        _plaintextSize = plaintextSize;
        _plaintextReadSize = maxReadSize;

        _streamEncrypt = streamEncryptionFactory.NewEncryptionStream(padSize);
        _headerBytes = _streamEncrypt.GenerateHeader(encryptionKey);
        _minimumBufferSize = LengthBufferSize + maxReadSize + padSize + (int)_streamEncrypt.TagSize;
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

        byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(LengthBufferSize);
        if (!_headerHasBeenReturned)
        {
            BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer.AsSpan()[..LengthBufferSize], _headerBytes.Length);
            lengthBuffer[..LengthBufferSize].CopyTo(buffer.AsMemory()[..LengthBufferSize]);
            ArrayPool<byte>.Shared.Return(lengthBuffer);
            _headerBytes.CopyTo(buffer.AsMemory()[LengthBufferSize..]);
            _headerHasBeenReturned = true;

            Console.WriteLine(Convert.ToHexString(buffer.AsSpan(0, _headerBytes.Length + LengthBufferSize)));

            return _headerBytes.Length + LengthBufferSize;
        }

        byte[] plaintextBuffer = ArrayPool<byte>.Shared.Rent(_plaintextReadSize);
        int bytesRead = _plaintextStream.Read(plaintextBuffer[.._plaintextReadSize], (int)_plaintextReadPosition, _plaintextReadSize);
        _plaintextReadPosition += bytesRead;
        _finishedReadingPlaintext = _plaintextReadPosition == _plaintextSize;

        byte[] ciphertext = bytesRead < _plaintextReadSize
            ? _streamEncrypt.Push(plaintextBuffer[..bytesRead], _finishedReadingPlaintext)
            : _streamEncrypt.Push(plaintextBuffer[.._plaintextReadSize], _finishedReadingPlaintext);
        ArrayPool<byte>.Shared.Return(plaintextBuffer);
        
        BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, ciphertext.Length);
        lengthBuffer[..LengthBufferSize].CopyTo(buffer.AsMemory()[..LengthBufferSize]);
        ArrayPool<byte>.Shared.Return(lengthBuffer);
        ciphertext.CopyTo(buffer.AsMemory()[LengthBufferSize..]);
        return ciphertext.Length + LengthBufferSize;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        AssertBufferSize(buffer.Length);

        if (_finishedReadingPlaintext)
        {
            return 0;
        }

        byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(LengthBufferSize);
        if (!_headerHasBeenReturned)
        {
            BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer.AsSpan()[..LengthBufferSize], _headerBytes.Length);
            lengthBuffer[..LengthBufferSize].CopyTo(buffer[..LengthBufferSize]);
            ArrayPool<byte>.Shared.Return(lengthBuffer);
            _headerBytes.CopyTo(buffer[LengthBufferSize..]);
            _headerHasBeenReturned = true;

            return _headerBytes.Length + LengthBufferSize;
        }

        byte[] plaintextBuffer = ArrayPool<byte>.Shared.Rent(_plaintextReadSize);
        int bytesRead = await _plaintextStream.ReadAsync(plaintextBuffer.AsMemory()[.._plaintextReadSize], cancellationToken);
        _plaintextReadPosition += bytesRead;
        _finishedReadingPlaintext = _plaintextReadPosition == _plaintextSize;

        byte[] ciphertext = bytesRead < _plaintextReadSize
            ? _streamEncrypt.Push(plaintextBuffer[..bytesRead], _finishedReadingPlaintext)
            : _streamEncrypt.Push(plaintextBuffer[.._plaintextReadSize], _finishedReadingPlaintext);
        ArrayPool<byte>.Shared.Return(plaintextBuffer);
        
        BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, ciphertext.Length);
        lengthBuffer[..LengthBufferSize].CopyTo(buffer[..LengthBufferSize]);
        ArrayPool<byte>.Shared.Return(lengthBuffer);
        ciphertext.CopyTo(buffer[LengthBufferSize..]);
        return ciphertext.Length + LengthBufferSize;
    }

    private void AssertBufferSize(int bufferSize)
    {
        if (bufferSize < _minimumBufferSize)
        {
            throw new ArgumentOutOfRangeException($"buffer size must be greater than or equal {_minimumBufferSize}");
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
        _plaintextStream.Close();
        base.Close();
    }
    
    protected override void Dispose(bool disposing)
    {
        _plaintextStream.Dispose();
        base.Dispose(disposing);
    }
    
    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _plaintextStream.DisposeAsync();
        await base.DisposeAsync();
    }
}
