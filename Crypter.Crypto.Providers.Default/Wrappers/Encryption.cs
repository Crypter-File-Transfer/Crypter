﻿/*
 * Copyright (C) 2025 Crypter File Transfer
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
using Crypter.Crypto.Common.Encryption;
using Geralt;

namespace Crypter.Crypto.Providers.Default.Wrappers
{
    [UnsupportedOSPlatform("browser")]
    public class Encryption : IEncryption
    {
        public uint KeySize => XChaCha20Poly1305.KeySize;
        public uint NonceSize => XChaCha20Poly1305.NonceSize;

        public byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext)
        {
            Span<byte> decryptedText = new byte[ciphertext.Length - XChaCha20Poly1305.TagSize];
            XChaCha20Poly1305.Decrypt(decryptedText, ciphertext, nonce, key);
            return decryptedText.ToArray();
        }

        public string DecryptToString(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> ciphertext)
        {
            byte[] decryptedKey = Decrypt(key, nonce, ciphertext);
            return Encoding.UTF8.GetString(decryptedKey);
        }

        public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, ReadOnlySpan<byte> plaintext)
        {
            Span<byte> encryptedText = new byte[plaintext.Length + XChaCha20Poly1305.TagSize];
            XChaCha20Poly1305.Encrypt(encryptedText, plaintext, nonce, key);
            return encryptedText.ToArray();
        }

        public byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, string plaintext)
        {
            Span<byte> plainTextBytes = Encoding.UTF8.GetBytes(plaintext);
            return Encrypt(key, nonce, plainTextBytes);
        }
    }
}
