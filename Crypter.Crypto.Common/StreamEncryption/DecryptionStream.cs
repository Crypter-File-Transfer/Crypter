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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Crypto.Common.StreamEncryption;

public class DecryptionStream : Stream
{
    private const int LengthBufferSize = sizeof(int);

    private readonly IStreamDecrypt _streamDecrypt;
    private readonly Stream _ciphertextStream;
    private readonly long _ciphertextStreamSize;

    private long _ciphertextReadPosition;
    private bool _finishedReadingCiphertext;

    /// <summary>
    /// Open a decryption stream in asynchronous scenarios.
    /// </summary>
    /// <param name="ciphertextStream"></param>
    /// <param name="streamSize"></param>
    /// <param name="decryptionKey"></param>
    /// <param name="streamEncryptionFactory"></param>
    /// <returns></returns>
    public static async Task<DecryptionStream> OpenAsync(
        Stream ciphertextStream,
        long streamSize,
        byte[] decryptionKey,
        IStreamEncryptionFactory streamEncryptionFactory)
    {
        int totalBytesRead = 0;
        
        byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(LengthBufferSize);
        totalBytesRead += await ciphertextStream.ReadAsync(lengthBuffer.AsMemory(0, LengthBufferSize));
        int headerSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer.AsSpan(0, LengthBufferSize));
        ArrayPool<byte>.Shared.Return(lengthBuffer);
        Console.WriteLine($"Header size is {headerSize} bytes");

        byte[] headerBuffer = ArrayPool<byte>.Shared.Rent(headerSize);
        totalBytesRead += await ciphertextStream.ReadAsync(headerBuffer.AsMemory(0, headerSize));
        IStreamDecrypt streamDecrypt = streamEncryptionFactory.NewDecryptionStream(decryptionKey, headerBuffer.AsSpan(0, headerSize));
        ArrayPool<byte>.Shared.Return(headerBuffer);
        
        return new DecryptionStream(ciphertextStream, streamSize, totalBytesRead, streamDecrypt);
    }
    
    private DecryptionStream(
        Stream ciphertextStream,
        long streamSize,
        long startingStreamPosition,
        IStreamDecrypt openStreamDecrypt)
    {
        _ciphertextStream = ciphertextStream;
        _ciphertextStreamSize = streamSize;
        _ciphertextReadPosition = startingStreamPosition;
        _streamDecrypt = openStreamDecrypt;
    }
    
    /// <summary>
    /// Open a decryption stream in synchronous scenarios.
    /// </summary>
    /// <param name="ciphertextStream"></param>
    /// <param name="streamSize"></param>
    /// <param name="decryptionKey"></param>
    /// <param name="streamEncryptionFactory"></param>
    public DecryptionStream(
        Stream ciphertextStream,
        long streamSize,
        Span<byte> decryptionKey,
        IStreamEncryptionFactory streamEncryptionFactory)
    {
        _ciphertextStream = ciphertextStream;
        _ciphertextStreamSize = streamSize;
        Span<byte> lengthBuffer = stackalloc byte[LengthBufferSize];
        _ciphertextReadPosition += ciphertextStream.Read(lengthBuffer);
        int headerSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        Span<byte> headerBuffer = stackalloc byte[headerSize];
        _ciphertextReadPosition += ciphertextStream.Read(headerBuffer);

        _streamDecrypt = streamEncryptionFactory.NewDecryptionStream(decryptionKey, headerBuffer);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _ciphertextStreamSize;

    public override long Position
    {
        get => _ciphertextReadPosition;
        set { throw new NotSupportedException(); }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(LengthBufferSize);
        int lengthBytesRead = _ciphertextStream.Read(lengthBuffer.AsSpan(0, LengthBufferSize));
        if (lengthBytesRead == 0)
        {
            return 0;
        }

        _ciphertextReadPosition += lengthBytesRead;
        int ciphertextChunkSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer.AsSpan(0, LengthBufferSize));
        ArrayPool<byte>.Shared.Return(lengthBuffer);
        int plaintextChunkSize = ciphertextChunkSize - (int)_streamDecrypt.TagSize;
        AssertBufferSize(buffer.Length, plaintextChunkSize);

        byte[] ciphertextBuffer = ArrayPool<byte>.Shared.Rent(ciphertextChunkSize);
        int bytesRead = _ciphertextStream.Read(ciphertextBuffer[..ciphertextChunkSize], 0, ciphertextChunkSize);
        _ciphertextReadPosition += bytesRead;
        _finishedReadingCiphertext = _ciphertextReadPosition == _ciphertextStreamSize;

        byte[] plaintext = _streamDecrypt.Pull(ciphertextBuffer.AsSpan(0, ciphertextChunkSize), plaintextChunkSize, out bool final);
        AssertFinal(final);
        plaintext.CopyTo(buffer.AsMemory());
        ArrayPool<byte>.Shared.Return(ciphertextBuffer);
        return plaintext.Length;
    }

    public override int Read(Span<byte> buffer)
    {
        Span<byte> lengthBuffer = stackalloc byte[LengthBufferSize];
        int lengthBytesRead = _ciphertextStream.Read(lengthBuffer);
        if (lengthBytesRead == 0)
        {
            return 0;
        }

        _ciphertextReadPosition += lengthBytesRead;
        int ciphertextChunkSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        int plaintextChunkSize = ciphertextChunkSize - (int)_streamDecrypt.TagSize;
        AssertBufferSize(buffer.Length, plaintextChunkSize);

        Span<byte> ciphertextBuffer = stackalloc byte[ciphertextChunkSize];
        int bytesRead = _ciphertextStream.Read(ciphertextBuffer);
        _ciphertextReadPosition += bytesRead;
        _finishedReadingCiphertext = _ciphertextReadPosition == _ciphertextStreamSize;

        byte[] plaintext = _streamDecrypt.Pull(ciphertextBuffer, plaintextChunkSize, out bool final);
        AssertFinal(final);
        plaintext.CopyTo(buffer);
        return plaintext.Length;
    }

    /// <summary>
    /// Read from the decryption stream asynchronously.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <remarks>This gets used during streamed web downloads!</remarks>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(LengthBufferSize);
        Console.WriteLine("Reading length from ciphertext stream");
        int lengthBytesRead = await _ciphertextStream.ReadAsync(lengthBuffer.AsMemory(0, LengthBufferSize), cancellationToken);
        if (lengthBytesRead == 0)
        {
            return 0;
        }
        Console.WriteLine($"Ciphertext length is {lengthBytesRead} bytes");
        
        _ciphertextReadPosition += lengthBytesRead;
        int ciphertextChunkSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer.AsSpan(0, LengthBufferSize));
        Console.WriteLine($"Chunk size is {ciphertextChunkSize} bytes long");
        ArrayPool<byte>.Shared.Return(lengthBuffer);
        int plaintextChunkSize = ciphertextChunkSize - (int)_streamDecrypt.TagSize;
        AssertBufferSize(buffer.Length, plaintextChunkSize);
        
        byte[] ciphertextBuffer = ArrayPool<byte>.Shared.Rent(ciphertextChunkSize);
        Console.WriteLine("Reading ciphertext from ciphertext stream");
        int ciphertextBytesRead = await _ciphertextStream.ReadAsync(ciphertextBuffer.AsMemory()[..ciphertextChunkSize], cancellationToken);
        Console.WriteLine($"Read {ciphertextBytesRead} ciphertext bytes from stream");
        _ciphertextReadPosition += ciphertextBytesRead;
        _finishedReadingCiphertext = _ciphertextReadPosition == _ciphertextStreamSize;
        
        Console.WriteLine("Decrypting chunk");
        byte[] plaintext = _streamDecrypt.Pull(ciphertextBuffer.AsSpan(0, ciphertextChunkSize), plaintextChunkSize, out bool final);
        Console.WriteLine("Decrypted chunk");
        AssertFinal(final);
        plaintext.CopyTo(buffer);
        ArrayPool<byte>.Shared.Return(ciphertextBuffer);
        return plaintext.Length;
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

    private static void AssertBufferSize(int bufferSize, int chunkSize)
    {
        if (bufferSize < chunkSize)
        {
            throw new ArgumentOutOfRangeException($"buffer size must be greater than or equal {chunkSize}");
        }
    }

    private void AssertFinal(bool final)
    {
        bool endOfStream = _finishedReadingCiphertext || _ciphertextReadPosition == _ciphertextStreamSize;
        if (endOfStream && !final)
        {
            throw new CryptographicException("Did not reach 'final' block as expected");
        }
        else if (final && !endOfStream)
        {
            throw new CryptographicException("Unexpected 'final' block");
        }
    }

    public override void Close()
    {
        _ciphertextStream.Close();
        base.Close();
    }
    
    protected override void Dispose(bool disposing)
    {
        _ciphertextStream.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _ciphertextStream.DisposeAsync();
        await base.DisposeAsync();
    }
}
