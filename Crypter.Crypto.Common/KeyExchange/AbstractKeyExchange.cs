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
using System.Text;
using Crypter.Crypto.Common.StreamGenericHash;

namespace Crypter.Crypto.Common.KeyExchange;

public abstract class AbstractKeyExchange : IKeyExchange
{
    private readonly byte[] _kxContext;
    private const string _kxContextString = "crypter";
    private readonly IStreamGenericHashFactory _streamGenericHashFactory;

    public uint SeedSize
    {
        get => 32;
    }

    public uint NonceSize
    {
        get => 32;
    }

    public uint ProofSize
    {
        get => 32;
    }

    public AbstractKeyExchange(IStreamGenericHashFactory streamGenericHashFactory)
    {
        _kxContext = Encoding.UTF8.GetBytes(_kxContextString);
        _streamGenericHashFactory = streamGenericHashFactory;
    }

    public (byte[] decryptionKey, byte[] proof) GenerateDecryptionKey(uint keySize, ReadOnlySpan<byte> privateKey,
        ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> nonce)
    {
        var (_, decryptionKey, proof) = GenerateTransmissionKeys(keySize, privateKey, publicKey, nonce);
        return (decryptionKey, proof);
    }

    public (byte[] encryptionKey, byte[] proof) GenerateEncryptionKey(uint keySize, ReadOnlySpan<byte> privateKey,
        ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> nonce)
    {
        var (encryptionKey, _, proof) = GenerateTransmissionKeys(keySize, privateKey, publicKey, nonce);
        return (encryptionKey, proof);
    }

    public abstract X25519KeyPair GenerateKeyPair();

    public abstract X25519KeyPair GenerateKeyPairDeterministic(ReadOnlySpan<byte> seed);

    public abstract byte[] GeneratePublicKey(ReadOnlySpan<byte> privateKey);

    public abstract byte[] GenerateSharedKey(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey);

    private (byte[] encryptionKey, byte[] decryptionKey, byte[] proof) GenerateTransmissionKeys(uint keySize,
        ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> publicKey, ReadOnlySpan<byte> nonce)
    {
        Span<byte> sharedKey = GenerateSharedKey(privateKey, publicKey);
        Span<byte> generatedPublicKey = GeneratePublicKey(privateKey);

        IStreamGenericHash encryptionKeyHasher = _streamGenericHashFactory.NewGenericHashStream(keySize, nonce);
        encryptionKeyHasher.Update(_kxContext);
        encryptionKeyHasher.Update(sharedKey);
        encryptionKeyHasher.Update(publicKey);
        encryptionKeyHasher.Update(generatedPublicKey);
        byte[] encryptionKey = encryptionKeyHasher.Complete();

        IStreamGenericHash decryptionKeyHasher = _streamGenericHashFactory.NewGenericHashStream(keySize, nonce);
        decryptionKeyHasher.Update(_kxContext);
        decryptionKeyHasher.Update(sharedKey);
        decryptionKeyHasher.Update(generatedPublicKey);
        decryptionKeyHasher.Update(publicKey);
        byte[] decryptionKey = decryptionKeyHasher.Complete();

        byte[] proof = GenerateProof(encryptionKey, decryptionKey, nonce);
        return (encryptionKey, decryptionKey, proof);
    }

    private byte[] GenerateProof(ReadOnlySpan<byte> encryptionKey, ReadOnlySpan<byte> decryptionKey,
        ReadOnlySpan<byte> nonce)
    {
        ReadOnlySpan<byte> firstKey = encryptionKey;
        ReadOnlySpan<byte> secondKey = decryptionKey;

        for (int i = 0; i < encryptionKey.Length; i++)
        {
            if (encryptionKey[i] < decryptionKey[i])
            {
                firstKey = encryptionKey;
                secondKey = decryptionKey;
                break;
            }

            if (encryptionKey[i] > decryptionKey[i])
            {
                firstKey = decryptionKey;
                secondKey = encryptionKey;
                break;
            }
        }

        IStreamGenericHash digestor = _streamGenericHashFactory.NewGenericHashStream(ProofSize, nonce);
        digestor.Update(firstKey);
        digestor.Update(secondKey);
        return digestor.Complete();
    }
}
