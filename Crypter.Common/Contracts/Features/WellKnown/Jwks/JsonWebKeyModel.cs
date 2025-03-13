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

using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace Crypter.Common.Contracts.Features.WellKnown.GetJwks
{
    public class JsonWebKeyModel
    {
        [JsonPropertyName(JsonWebKeyParameterNames.Alg)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Alg { get; private init; }

        [JsonPropertyName(JsonWebKeyParameterNames.Crv)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Crv { get; private init; }

        [JsonPropertyName(JsonWebKeyParameterNames.Kid)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Kid { get; private init; }

        [JsonPropertyName(JsonWebKeyParameterNames.Kty)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Kty { get; private init; }

        [JsonPropertyName(JsonWebKeyParameterNames.X)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string X { get; private init; }

        [JsonPropertyName(JsonWebKeyParameterNames.Use)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Use { get; private init; }

        [JsonConstructor]
        public JsonWebKeyModel(string alg, string crv, string kid, string kty, string x, string use)
        {
            Alg = alg;
            Crv = crv;
            Kid = kid;
            Kty = kty;
            X = x;
            Use = use;
        }

        public JsonWebKeyModel(JsonWebKey jwk)
        {
            Alg = jwk.Alg;
            Crv = jwk.Crv;
            Kid = jwk.Kid;
            Kty = jwk.Kty;
            X = jwk.X;
            Use = jwk.Use;
        }
    }
}
