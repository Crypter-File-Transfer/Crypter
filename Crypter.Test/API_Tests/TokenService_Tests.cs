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
using Crypter.API.Services;
using NUnit.Framework;
using System;

namespace Crypter.Test.API_Tests
{
   [TestFixture]
   public class TokenService_Tests
   {
      private TokenSettings _tokenSettings;

      [OneTimeSetUp]
      public void OneTimeSetup()
      {
         _tokenSettings = new TokenSettings
         {
            Audience = "The Fellowship",
            Issuer = "Legolas",
            SecretKey = "They're taking the hobbits to Isengard!",
            AuthenticationLifetimeMinutes = 5,
            SessionLifetimeMinutes = 30,
            RefreshLifetimeDays = 5
         };
      }

      [Test]
      public void Can_Create_And_Validate_Authentication_Token()
      {
         var userId = Guid.NewGuid();

         var sut = new TokenService(_tokenSettings);
         var token = sut.NewAuthenticationToken(userId);

         Assert.IsNotNull(token);
         Assert.IsNotEmpty(token);

         var maybeClaimsPrincipal = sut.ValidateToken(token);
         Assert.IsTrue(maybeClaimsPrincipal.IsSome);
         Assert.IsNotNull(maybeClaimsPrincipal.ValueUnsafe);

         var parsedUserId = sut.ParseUserId(maybeClaimsPrincipal.ValueUnsafe);
         Assert.AreEqual(userId, parsedUserId);
      }

      [Test]
      public void Can_Create_And_Validate_Session_Token()
      {
         var userId = Guid.NewGuid();
         var tokenId = Guid.NewGuid();

         var sut = new TokenService(_tokenSettings);
         var tokenCreatedUTC = DateTime.UtcNow;
         var expectedTokenExpiration = tokenCreatedUTC.AddMinutes(_tokenSettings.SessionLifetimeMinutes);
         var (token, actualTokenExpiration) = sut.NewSessionToken(userId, tokenId);

         Assert.IsNotNull(token);
         Assert.IsNotEmpty(token);

         var maybeClaimsPrincipal = sut.ValidateToken(token);
         Assert.IsTrue(maybeClaimsPrincipal.IsSome);
         Assert.IsNotNull(maybeClaimsPrincipal.ValueUnsafe);

         var parsedUserId = sut.ParseUserId(maybeClaimsPrincipal.ValueUnsafe);
         Assert.AreEqual(userId, parsedUserId);

         var maybeTokenId = sut.ParseTokenId(maybeClaimsPrincipal.ValueUnsafe);
         Assert.IsTrue(maybeTokenId.IsSome);
         Assert.AreEqual(tokenId, maybeTokenId.ValueUnsafe);

         Assert.AreEqual(expectedTokenExpiration.Ticks, actualTokenExpiration.Ticks, TimeSpan.TicksPerSecond);
      }

      [Test]
      public void Can_Create_And_Validate_Refresh_Token()
      {
         var userId = Guid.NewGuid();
         var tokenId = Guid.NewGuid();

         var sut = new TokenService(_tokenSettings);
         var tokenCreatedUTC = DateTime.UtcNow;
         var expectedTokenExpiration = tokenCreatedUTC.AddDays(_tokenSettings.RefreshLifetimeDays);
         var (token, actualTokenExpiration) = sut.NewRefreshToken(userId, tokenId);

         Assert.IsNotNull(token);
         Assert.IsNotEmpty(token);

         var maybeClaimsPrincipal = sut.ValidateToken(token);
         Assert.IsTrue(maybeClaimsPrincipal.IsSome);
         Assert.IsNotNull(maybeClaimsPrincipal.ValueUnsafe);

         var parsedUserId = sut.ParseUserId(maybeClaimsPrincipal.ValueUnsafe);
         Assert.AreEqual(userId, parsedUserId);

         var maybeTokenId = sut.ParseTokenId(maybeClaimsPrincipal.ValueUnsafe);
         Assert.IsTrue(maybeTokenId.IsSome);
         Assert.AreEqual(tokenId, maybeTokenId.ValueUnsafe);

         Assert.AreEqual(expectedTokenExpiration.Ticks, actualTokenExpiration.Ticks, TimeSpan.TicksPerSecond);
      }
   }
}
