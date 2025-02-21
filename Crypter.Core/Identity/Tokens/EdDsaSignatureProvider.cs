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

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;

namespace Crypter.Core.Identity.Tokens
{
    internal class EdDsaCryptoProvider : ICryptoProvider
    {
        public object Create(string algorithm, params object[] args)
        {
            if (algorithm == EdDsaAlgorithm.Name && args[0] is EdDsaSecurityKey key)
            {
                return new EdDsaSignatureProvider(key, algorithm);
            }

            throw new NotSupportedException($"Only {EdDsaAlgorithm.Name} algorithm is supported.");
        }

        public bool IsSupportedAlgorithm(string algorithm, params object[] args) => algorithm == EdDsaAlgorithm.Name;

        public void Release(object cryptoInstance)
        {
            if (cryptoInstance is IDisposable disposableObject)
                disposableObject.Dispose();
        }
    }

    internal class EdDsaSignatureProvider : SignatureProvider
    {
        private readonly EdDsaSecurityKey edDsaKey;

        public EdDsaSignatureProvider(EdDsaSecurityKey key, string algorithm)
            : base(key, algorithm)
        {
            edDsaKey = key;
            WillCreateSignatures = key.PrivateKeyStatus == PrivateKeyStatus.Exists;
        }

        protected override void Dispose(bool disposing)
        {
        }

        public override byte[] Sign(byte[] input) => edDsaKey.Algorithm.Sign(input);

        public override bool Sign(ReadOnlySpan<byte> data, Span<byte> destination, out int bytesWritten)
        {
            var signature = edDsaKey.Algorithm.Sign(data.ToArray());
            signature.CopyTo(destination);
            bytesWritten = signature.Length;
            return true;
        }

        public override byte[] Sign(byte[] input, int offset, int count)
        {
            var data = new byte[count];
            Buffer.BlockCopy(input, offset, data, 0, count);
            return edDsaKey.Algorithm.Sign(data);
        }

        public override bool Verify(byte[] input, byte[] signature) => edDsaKey.Algorithm.Verify(input, signature);
        
        public override bool Verify(byte[] input, int inputOffset, int inputLength, byte[] signature, int signatureOffset, int signatureLength) 
            => edDsaKey.Algorithm.Verify(input, inputOffset, inputLength, signature, signatureOffset, signatureLength);

    }
}
