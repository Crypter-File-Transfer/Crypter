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
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Crypto.Common.StreamEncryption;

public class DecryptionStream : Stream
{
    private const int _lengthBufferSize = sizeof(int);

    private readonly IStreamDecrypt _streamDecrypt;
    private readonly Stream _ciphertextStream;
    private readonly long _ciphertextStreamSize;

    private long _ciphertextReadPosition;
    private bool _finishedReadingCiphertext;

    public DecryptionStream(Stream ciphertextStream, long streamSize, Span<byte> decryptionKey,
        IStreamEncryptionFactory streamEncryptionFactory)
    {
        _ciphertextStream = ciphertextStream;
        _ciphertextStreamSize = streamSize;
        Span<byte> lengthBuffer = stackalloc byte[_lengthBufferSize];
        _ciphertextReadPosition += ciphertextStream.Read(lengthBuffer);
        int headerSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        Span<byte> headerBuffer = stackalloc byte[headerSize];
        _ciphertextReadPosition += ciphertextStream.Read(headerBuffer);

        _streamDecrypt = streamEncryptionFactory.NewDecryptionStream(decryptionKey, headerBuffer);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotImplementedException();

    public override long Position
    {
        get { throw new NotSupportedException(); }
        set { throw new NotSupportedException(); }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        byte[] lengthBuffer = new byte[_lengthBufferSize];
        int lengthBytesRead = _ciphertextStream.Read(lengthBuffer);
        if (lengthBytesRead == 0)
        {
            return 0;
        }

        _ciphertextReadPosition += lengthBytesRead;
        int ciphertextChunkSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        int plaintextChunkSize = ciphertextChunkSize - (int)_streamDecrypt.TagSize;
        AssertBufferSize(buffer.Length, plaintextChunkSize);

        byte[] ciphertextBuffer = new byte[ciphertextChunkSize];
        int bytesRead = _ciphertextStream.Read(ciphertextBuffer, 0, ciphertextChunkSize);
        _ciphertextReadPosition += bytesRead;
        _finishedReadingCiphertext = _ciphertextReadPosition == _ciphertextStreamSize;

        byte[] plaintext = _streamDecrypt.Pull(ciphertextBuffer, plaintextChunkSize, out bool final);
        AssertFinal(final);
        plaintext.CopyTo(buffer.AsMemory());
        return plaintext.Length;
    }

    public override int Read(Span<byte> buffer)
    {
        Span<byte> lengthBuffer = new byte[_lengthBufferSize];
        int lengthBytesRead = _ciphertextStream.Read(lengthBuffer);
        if (lengthBytesRead == 0)
        {
            return 0;
        }

        _ciphertextReadPosition += lengthBytesRead;
        int ciphertextChunkSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        int plaintextChunkSize = ciphertextChunkSize - (int)_streamDecrypt.TagSize;
        AssertBufferSize(buffer.Length, plaintextChunkSize);

        Span<byte> ciphertextBuffer = new byte[ciphertextChunkSize];
        int bytesRead = _ciphertextStream.Read(ciphertextBuffer);
        _ciphertextReadPosition += bytesRead;
        _finishedReadingCiphertext = _ciphertextReadPosition == _ciphertextStreamSize;

        byte[] plaintext = _streamDecrypt.Pull(ciphertextBuffer, plaintextChunkSize, out bool final);
        AssertFinal(final);
        plaintext.CopyTo(buffer);
        return plaintext.Length;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        byte[] lengthBuffer = new byte[_lengthBufferSize];
        int lengthBytesRead = await _ciphertextStream.ReadAsync(lengthBuffer.AsMemory(), cancellationToken);
        if (lengthBytesRead == 0)
        {
            return 0;
        }

        _ciphertextReadPosition += lengthBytesRead;
        int ciphertextChunkSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        int plaintextChunkSize = ciphertextChunkSize - (int)_streamDecrypt.TagSize;
        AssertBufferSize(buffer.Length, plaintextChunkSize);

        byte[] ciphertextBuffer = new byte[ciphertextChunkSize];
        int bytesRead = await _ciphertextStream.ReadAsync(
            ciphertextBuffer.AsMemory((int)_ciphertextReadPosition, ciphertextChunkSize), cancellationToken);
        _ciphertextReadPosition += bytesRead;
        _finishedReadingCiphertext = _ciphertextReadPosition == _ciphertextStreamSize;

        byte[] plaintext = _streamDecrypt.Pull(ciphertextBuffer, plaintextChunkSize, out bool final);
        AssertFinal(final);
        plaintext.CopyTo(buffer);
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

    protected override void Dispose(bool disposing)
    {
        _ciphertextStream.Dispose();
        base.Dispose(disposing);
    }
}
