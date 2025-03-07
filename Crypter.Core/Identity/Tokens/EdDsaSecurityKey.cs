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

using Geralt;
using Microsoft.IdentityModel.Tokens;
using System;

namespace Crypter.Core.Identity.Tokens
{
    public class EdDsaSecurityKey : AsymmetricSecurityKey
    {
        public const string JsonWebKeyKeyType_OctetKeyPair = "OKP";

        public EdDsaAlgorithm Algorithm { get; }

        public EdDsaSecurityKey(EdDsaAlgorithm algorithm)
        {
            Algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            CryptoProviderFactory.CustomCryptoProvider = new EdDsaCryptoProvider();
        }

        [Obsolete("HasPrivateKey method is deprecated, please use PrivateKeyStatus.")] 
        public override bool HasPrivateKey => Algorithm?.KeyPair.PrivateKey != null;

        public override PrivateKeyStatus PrivateKeyStatus =>
            Algorithm?.KeyPair.PrivateKey != null ? PrivateKeyStatus.Exists : PrivateKeyStatus.DoesNotExist;

        public override int KeySize => Algorithm.KeySize;

        public JsonWebKey AsJWK()
        {
            return new JsonWebKey
            {
                Crv = nameof(Ed25519),
                X = Algorithm.KeyPair?.PublicKey != null ? Base64UrlEncoder.Encode(Algorithm.KeyPair?.PublicKey) : null,
                D = Algorithm.KeyPair?.PrivateKey != null ? Base64UrlEncoder.Encode(Algorithm.KeyPair?.PrivateKey) : null,
                Kty = JsonWebKeyKeyType_OctetKeyPair,
                Alg = EdDsaAlgorithm.Name,
                CryptoProviderFactory = CryptoProviderFactory,
            };
        }
    }
}
