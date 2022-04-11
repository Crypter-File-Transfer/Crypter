/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.API.Models;
using Crypter.API.Startup;
using Crypter.Common.Exceptions;
using Crypter.Common.Monads;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Crypter.API.Services
{
   public interface ITokenService
   {
      string NewAuthenticationToken(Guid userId);
      (string token, DateTime expiration) NewSessionToken(Guid userId, Guid tokenId);
      (string token, DateTime expiration) NewRefreshToken(Guid userId, Guid tokenId);
      Maybe<ClaimsPrincipal> ValidateToken(string token);
      Guid ParseUserId(ClaimsPrincipal claimsPrincipal);
      Maybe<Guid> ParseTokenId(ClaimsPrincipal claimsPrincipal);
   }

   public class TokenService : ITokenService
   {
      private readonly TokenSettings _tokenSettings;

      public TokenService(TokenSettings tokenSettings)
      {
         _tokenSettings = tokenSettings;
      }

      public string NewAuthenticationToken(Guid userId)
      {
         var expiration = DateTime.UtcNow.AddMinutes(_tokenSettings.AuthenticationLifetimeMinutes);
         return NewToken(userId, expiration);
      }

      public (string token, DateTime expiration) NewSessionToken(Guid userId, Guid tokenId)
      {
         var expiration = DateTime.UtcNow.AddMinutes(_tokenSettings.SessionLifetimeMinutes);
         return (NewToken(userId, expiration, tokenId), expiration);
      }

      public (string token, DateTime expiration) NewRefreshToken(Guid userId, Guid tokenId)
      {
         var expiration = DateTime.UtcNow.AddDays(_tokenSettings.RefreshLifetimeDays);
         return (NewToken(userId, expiration, tokenId), expiration);
      }

      public Maybe<ClaimsPrincipal> ValidateToken(string token)
      {
         var validationParameters = JwtBearerConfiguration.GetTokenValidationParameters(_tokenSettings);

         try
         {
            return new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
         }
         catch (Exception)
         {
            return Maybe<ClaimsPrincipal>.None;
         }
      }

      public Guid ParseUserId(ClaimsPrincipal claimsPrincipal)
      {
         var userClaim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
         if (userClaim is null || !Guid.TryParse(userClaim.Value, out Guid userId))
         {
            throw new InvalidTokenException();
         }

         return userId;
      }

      public Maybe<Guid> ParseTokenId(ClaimsPrincipal claimsPrincipal)
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

         var tokenKeyBytes = Encoding.UTF8.GetBytes(_tokenSettings.SecretKey);
         var tokenDescriptor = new SecurityTokenDescriptor
         {
            Subject = claims,
            Audience = _tokenSettings.Audience,
            Issuer = _tokenSettings.Issuer,
            Expires = expiration,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKeyBytes), SecurityAlgorithms.HmacSha256Signature)
         };
         var token = tokenHandler.CreateToken(tokenDescriptor);
         return tokenHandler.WriteToken(token);
      }
   }
}
