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
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Crypter.Common.Exceptions;
using Crypter.Core.Identity;
using Crypter.Core.Identity.Tokens;
using EasyMonads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ICrypterCryptoProvider = Crypter.Crypto.Common.ICryptoProvider;

namespace Crypter.Core.Services;

public interface ITokenService
{
    string NewAuthenticationToken(Guid userId);
    RefreshTokenData NewSessionToken(Guid userId);
    RefreshTokenData NewDeviceToken(Guid userId);
    Maybe<ClaimsPrincipal> ValidateToken(string token);
    JsonWebKey PublicJWK();
    TokenValidationParameters TokenValidationParameters { get; }
}

public static class TokenServiceExtensions
{
    public static void AddTokenService(this IServiceCollection services, Action<TokenSettings> settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        services.Configure(settings);
        services.TryAddSingleton<ITokenService, TokenService>();
    }
}

public class TokenService : ITokenService
{
    private readonly TokenSettings _tokenSettings;
    private readonly ITokenKeyProvider _tokenKeyProvider;

    public TokenValidationParameters TokenValidationParameters
    {
        get
        {
            var parameters = TokenParametersProvider.GetTokenValidationParameters(_tokenSettings);
            parameters.IssuerSigningKey = _tokenKeyProvider.PublicKey;
            return parameters;
        }
    }

    public TokenService(IOptions<TokenSettings> tokenSettings, ICrypterCryptoProvider cryptoProvider)
    {
        _tokenSettings = tokenSettings.Value;
        _tokenKeyProvider = new TokenKeyProvider(cryptoProvider, _tokenSettings);
    }

    public string NewAuthenticationToken(Guid userId)
    {
        var expiration = DateTime.UtcNow.AddMinutes(_tokenSettings.AuthenticationTokenLifetimeMinutes);
        return NewToken(userId, expiration);
    }

    public RefreshTokenData NewSessionToken(Guid userId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime expiration = now.AddMinutes(_tokenSettings.SessionTokenLifetimeMinutes);
        return NewRefreshToken(userId, now, expiration);
    }

    public RefreshTokenData NewDeviceToken(Guid userId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime expiration = now.AddDays(_tokenSettings.DeviceTokenLifetimeDays);
        return NewRefreshToken(userId, now, expiration);
    }

    public Maybe<ClaimsPrincipal> ValidateToken(string token)
    {
        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, TokenValidationParameters, out _);
        }
        catch (Exception)
        {
            return Maybe<ClaimsPrincipal>.None;
        }
    }

    public static Guid ParseUserId(ClaimsPrincipal claimsPrincipal)
    {
        return TryParseUserId(claimsPrincipal).Match(
            () => throw new InvalidTokenException(),
            x => x);
    }

    public static Maybe<Guid> TryParseUserId(ClaimsPrincipal claimsPrincipal)
    {
        var userClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        if (userClaim is null || !Guid.TryParse(userClaim.Value, out Guid userId))
        {
            return Maybe<Guid>.None;
        }

        return userId;
    }

    public static Maybe<Guid> TryParseTokenId(ClaimsPrincipal claimsPrincipal)
    {
        var idClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti);
        if (idClaim is null)
        {
            return Maybe<Guid>.None;
        }

        return Guid.TryParse(idClaim.Value, out Guid tokenId)
            ? tokenId
            : Maybe<Guid>.None;
    }

    private RefreshTokenData NewRefreshToken(Guid userId, DateTime created, DateTime expiration)
    {
        Guid tokenId = Guid.NewGuid();
        string token = NewToken(userId, expiration, tokenId);
        return new RefreshTokenData(token, tokenId, created, expiration);
    }

    private string NewToken(Guid userId, DateTime expiration, Guid tokenId = default)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var claims = new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        });

        if (tokenId != default)
        {
            var jtiClaim = new Claim(JwtRegisteredClaimNames.Jti, tokenId.ToString());
            claims.AddClaim(jtiClaim);
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claims,
            Audience = _tokenSettings.Audience,
            Issuer = _tokenSettings.Issuer,
            Expires = expiration,
            SigningCredentials = new SigningCredentials(_tokenKeyProvider.PrivateKey, EdDsaAlgorithm.Name)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public JsonWebKey PublicJWK()
    {
        JsonWebKey key = _tokenKeyProvider.PublicJWK;
        key.D = null; // that should be private
        key.Use = JsonWebKeyUseNames.Sig;
        return key;
    }
}
