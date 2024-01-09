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

using System.Collections.Generic;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Models;
using Crypter.Common.Enums;
using EasyMonads;

namespace Crypter.Common.Client.Repositories;

public class MemoryTokenRepository : ITokenRepository
{
    private const string AuthTokenLiteral = "authToken";
    private const string RefreshTokenLiteral = "refreshToken";
    private readonly Dictionary<string, object> _repository = new Dictionary<string, object>();

    public Task<Maybe<TokenObject>> GetAuthenticationTokenAsync()
    {
        return _repository.TryGetValue(AuthTokenLiteral, out object? value)
            ? Maybe<TokenObject>.From((TokenObject)value).AsTask()
            : Maybe<TokenObject>.None.AsTask();
    }

    public Task<Maybe<TokenObject>> GetRefreshTokenAsync()
    {
        return _repository.TryGetValue(RefreshTokenLiteral, out object? value)
            ? Maybe<TokenObject>.From((TokenObject)value).AsTask()
            : Maybe<TokenObject>.None.AsTask();
    }

    public Task<Unit> StoreAuthenticationTokenAsync(string token)
    {
        _repository.Remove(AuthTokenLiteral);
        TokenObject tokenObject = new TokenObject(TokenType.Authentication, token);
        _repository.Add(AuthTokenLiteral, tokenObject);
        return Unit.Default.AsTask();
    }

    public Task<Unit> StoreRefreshTokenAsync(string token, TokenType tokenType)
    {
        _repository.Remove(RefreshTokenLiteral);
        TokenObject tokenObject = new TokenObject(tokenType, token);
        _repository.Add(RefreshTokenLiteral, tokenObject);
        return Unit.Default.AsTask();
    }
}
