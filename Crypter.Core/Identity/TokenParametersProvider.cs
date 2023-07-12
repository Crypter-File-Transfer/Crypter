using System;
using Microsoft.IdentityModel.Tokens;
using ScottBrady.IdentityModel.Crypto;
using ScottBrady.IdentityModel.Tokens;

namespace Crypter.Core.Identity
{
   public static class TokenParametersProvider
   {
      public static TokenValidationParameters GetTokenValidationParameters(TokenSettings tokenSettings)
      {
         var privateKeyBytes = Convert.FromBase64String(tokenSettings.PrivateKey);
         var edDsa = EdDsa.Create(new EdDsaParameters(ExtendedSecurityAlgorithms.Curves.Ed25519)
         {
            D = privateKeyBytes
         });

         var edDsaSecurityKey = new EdDsaSecurityKey(edDsa);

         return new TokenValidationParameters
         {
            ValidateAudience = true,
            ValidAudience = tokenSettings.Audience,
            ValidIssuer = tokenSettings.Issuer,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = edDsaSecurityKey,
            ValidateLifetime = true,
            ClockSkew = TokenValidationParameters.DefaultClockSkew,
            RequireExpirationTime = true,
            ValidAlgorithms = new[] { "EdDSA" }
         };
      }
   }
}
