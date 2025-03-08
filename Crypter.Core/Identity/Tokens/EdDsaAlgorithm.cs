/*
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
using System.Linq;
using System.Security.Cryptography;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.DigitalSignature;
using Geralt;

namespace Crypter.Core.Identity.Tokens
{
    public class EdDsaAlgorithm : AsymmetricAlgorithm
    {
        public const string Name = "EdDSA";

        internal Ed25519KeyPair KeyPair { get; }
        private ICryptoProvider CryptoProvider { get; }
        
        private EdDsaAlgorithm(ICryptoProvider signer)
        {
            CryptoProvider = signer;
            KeyPair = CryptoProvider.DigitalSignature.GenerateKeyPair();
        }

        private EdDsaAlgorithm(ICryptoProvider signer, ReadOnlySpan<byte> seed)
        {
            CryptoProvider = signer;
            KeyPair = CryptoProvider.DigitalSignature.GenerateKeyPair(seed);
        }

        public static EdDsaAlgorithm Create(ICryptoProvider? cryptoProvider)
        {
            return cryptoProvider == null ? throw new ArgumentNullException(nameof(cryptoProvider)) : new EdDsaAlgorithm(cryptoProvider);
        }

        public static EdDsaAlgorithm Create(ICryptoProvider? cryptoProvider, ReadOnlySpan<byte> seed)
        {
            return cryptoProvider == null ? throw new ArgumentNullException(nameof(cryptoProvider)) : new EdDsaAlgorithm(cryptoProvider, seed);
        }

        public override string SignatureAlgorithm => Name;

        public override KeySizes[] LegalKeySizes => [new KeySizes(Ed25519.PublicKeySize, Ed25519.PrivateKeySize, 32)];

        public override int KeySize => KeyPair.PrivateKey.Length > 0 ? KeyPair.PrivateKey.Length : KeyPair.PublicKey.Length;

        public byte[] Sign(byte[] input) => CryptoProvider.DigitalSignature.GenerateSignature(KeyPair.PrivateKey, input);

        public bool Verify(byte[] input, byte[] signature) => CryptoProvider.DigitalSignature.VerifySignature(KeyPair.PublicKey, input, signature);

        public bool Verify(byte[] input, int inputOffset, int inputLength, byte[] signature, int signatureOffset, int signatureLength)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(signature);
            if (inputLength <= 0) throw new ArgumentException($"{nameof(inputLength)} must be greater than 0");
            if (signatureLength <= 0) throw new ArgumentException($"{nameof(signatureLength)} must be greater than 0");

            return Verify(input.Skip(inputOffset).Take(inputLength).ToArray(), signature.Skip(signatureOffset).Take(signatureLength).ToArray());
        }
    }
}
