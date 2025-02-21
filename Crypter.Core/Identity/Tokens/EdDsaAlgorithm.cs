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
using System.IO;
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

        internal Ed25519KeyPair KeyPair { get; private set; }
        internal ICryptoProvider CryptoProvider { get; private set; }
        
        private EdDsaAlgorithm(ICryptoProvider signer)
        {
            CryptoProvider = signer;
            KeyPair = CryptoProvider.DigitalSignature.GenerateKeyPair();
        }

        private EdDsaAlgorithm(ICryptoProvider signer, Ed25519KeyPair keyPair)
        {
            CryptoProvider = signer;
            KeyPair = keyPair;
        }

        public static EdDsaAlgorithm Create(ICryptoProvider? cryptoProvider)
        {
            return cryptoProvider == null ? throw new ArgumentNullException(nameof(cryptoProvider)) : new EdDsaAlgorithm(cryptoProvider);
        }

        public override string SignatureAlgorithm => Name;

        public override KeySizes[] LegalKeySizes => [new KeySizes(32, 32, 0)];

        public override int KeySize => KeyPair?.PrivateKey?.Length ?? KeyPair?.PublicKey?.Length ?? throw new InvalidOperationException("Missing EdDsa key");

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

        public static EdDsaAlgorithm FromPrivateKeyFile(string privateKeyFilePath, string? passPhrase, ICryptoProvider cryptoProvider)
        {
            try
            {
                using StreamReader streamReader = new StreamReader(privateKeyFilePath);
                string[]? encodedPrivateKey = streamReader.ReadLine()?.TrimStart().Split(" ");
                if (encodedPrivateKey != null)
                {
                    byte[] publicKey = new byte[Ed25519.PublicKeySize];
                    byte[] decodedPrivateKey = passPhrase != null ?
                        cryptoProvider.Encryption.Decrypt(passPhrase, Encodings.FromBase64(encodedPrivateKey[0])) :
                        Encodings.FromBase64(encodedPrivateKey[0]);
                    
                    Ed25519.ComputePublicKey(publicKey, decodedPrivateKey);
                    return new EdDsaAlgorithm(cryptoProvider, new Ed25519KeyPair(decodedPrivateKey, publicKey));                    
                }
                throw new InvalidDataException("Unable to process private key file.");
            }
            catch (Exception ex)
            {
                throw new IOException("Unable to read/process the private key file.", ex);
            }
        }

        public void TryExportPrivateKey(string filePath, string? passPhrase)
        {
            if (Path.GetDirectoryName(filePath) is string directory) {
                Directory.CreateDirectory(directory);
            }

            string? encodedPrivateKey = passPhrase != null ?
                Encodings.ToBase64(CryptoProvider.Encryption.Encrypt(passPhrase, KeyPair.PrivateKey)) :
                Encodings.ToBase64(KeyPair.PrivateKey);            

            File.WriteAllText(filePath, encodedPrivateKey!);
        }
    }
}
