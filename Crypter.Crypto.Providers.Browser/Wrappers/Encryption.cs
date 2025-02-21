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
using System.Runtime.Versioning;
using System.Text;
using BlazorSodium.Sodium;
using Crypter.Crypto.Common.Encryption;
using Crypter.Crypto.Common.PasswordHash;
using EasyMonads;
using SodiumPasswordHash = BlazorSodium.Sodium.PasswordHash;

namespace Crypter.Crypto.Providers.Browser.Wrappers;

[SupportedOSPlatform("browser")]
public class Encryption : IEncryption
{
    public uint KeySize
    {
        get => SecretBox.KEY_BYTES;
    }

    public uint NonceSize
    {
        get => SecretBox.NONCE_BYTES;
    }

    private readonly IPasswordHash _passwordHash;

    public Encryption(IPasswordHash passwordHash)
    {
        _passwordHash = passwordHash;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext)
    {
        return AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Decrypt(ciphertext.ToArray(), nonce.ToArray(), key.ToArray(),
            (byte[]?)null);
    }

    public byte[] Decrypt(string passPhrase, ReadOnlySpan<byte> cipherText)
    {
        Span<byte> nonce = new byte[NonceSize];
        nonce.Clear();
        ReadOnlySpan<byte> salt = cipherText[..(int)SodiumPasswordHash.SALT_BYTES];

        Either<Exception, byte[]> eitherKey = _passwordHash.GenerateKey(passPhrase, salt, KeySize, OpsLimit.Sensitive, MemLimit.Moderate);
        byte[]? key = eitherKey.ToMaybe().SomeOrDefault();
        return Decrypt(key!, nonce, cipherText);
    } 

    public string DecryptToString(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext)
    {
        byte[] plaintext =
            AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Decrypt(ciphertext.ToArray(), nonce.ToArray(), key.ToArray(),
                (byte[]?)null);
        return Encoding.UTF8.GetString(plaintext);
    }

    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext)
    {
        return AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Encrypt(plaintext.ToArray(), nonce.ToArray(), key.ToArray(),
            (byte[]?)null);
    }

    public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, string plaintext)
    {
        return AEAD.Crypto_AEAD_XChaCha20Poly1305_IETF_Encrypt(plaintext, nonce.ToArray(), key.ToArray(), (byte[]?)null);
    }

    public byte[] Encrypt(string passPhrase, ReadOnlySpan<byte> plainText)
    {
        Span<byte> salt = RandomBytes.RandomBytes_Buf(SodiumPasswordHash.SALT_BYTES);        
        Span<byte> nonce = new byte[NonceSize];
        nonce.Clear();

        Either<Exception, byte[]> eitherKey = _passwordHash.GenerateKey(passPhrase, salt, KeySize, OpsLimit.Sensitive, MemLimit.Moderate);
        byte[]? key = eitherKey.ToMaybe().SomeOrDefault();

        Span<byte> cipherText = Encrypt(key, nonce, plainText);
        Span<byte> cipherWithSalt = new byte[salt.Length + cipherText.Length];

        salt.CopyTo(cipherWithSalt[..salt.Length]);
        cipherText.CopyTo(cipherWithSalt.Slice(start:salt.Length, cipherText.Length));
        
        return cipherWithSalt.ToArray();
    }
}
