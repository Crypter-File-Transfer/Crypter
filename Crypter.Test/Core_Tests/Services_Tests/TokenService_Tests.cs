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

using Crypter.Core.Services;
using Crypter.Test.Core_Tests.Services_Tests.Helpers;
using NUnit.Framework;
using System;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   public class TokenService_Tests
   {
      [Test]
      public void Can_Create_And_Validate_Authentication_Token()
      {
         var userId = Guid.NewGuid();
         var sut = ServiceTestFactory.GetTokenService();
         var token = sut.NewAuthenticationToken(userId);

         Assert.IsNotNull(token);
         Assert.IsNotEmpty(token);

         var maybeClaimsPrincipal = sut.ValidateToken(token);
         maybeClaimsPrincipal.IfNone(Assert.Fail);
         maybeClaimsPrincipal.IfSome(x =>
         {
            Assert.IsNotNull(x);
            var parsedUserId = sut.ParseUserId(x);
            Assert.AreEqual(userId, parsedUserId);
         });
      }

      [Test]
      public void Can_Create_And_Validate_Session_Token()
      {
         var userId = Guid.NewGuid();

         var tokenOptions = ServiceTestFactory.GetDefaultTokenOptions();
         var sut = new TokenService(tokenOptions);
         var tokenCreatedUTC = DateTime.UtcNow;
         var expectedTokenExpiration = tokenCreatedUTC.AddMinutes(tokenOptions.Value.SessionTokenLifetimeMinutes);
         var tokenData = sut.NewSessionToken(userId);

         Assert.IsNotNull(tokenData);
         Assert.IsNotEmpty(tokenData.Token);
         Assert.AreEqual(expectedTokenExpiration.Ticks, tokenData.Expiration.Ticks, TimeSpan.TicksPerSecond);

         var maybeClaimsPrincipal = sut.ValidateToken(tokenData.Token);
         maybeClaimsPrincipal.IfNone(Assert.Fail);
         maybeClaimsPrincipal.IfSome(claimsPrincipal =>
         {
            Assert.IsNotNull(claimsPrincipal);

            var parsedUserId = sut.ParseUserId(claimsPrincipal);
            Assert.AreEqual(userId, parsedUserId);

            var maybeTokenId = sut.TryParseTokenId(claimsPrincipal);
            maybeTokenId.IfNone(Assert.Fail);
         });
      }

      [Test]
      public void Can_Create_And_Validate_Device_Token()
      {
         var userId = Guid.NewGuid();

         var tokenOptions = ServiceTestFactory.GetDefaultTokenOptions();
         var sut = new TokenService(tokenOptions);
         var tokenCreatedUTC = DateTime.UtcNow;
         var expectedTokenExpiration = tokenCreatedUTC.AddDays(tokenOptions.Value.DeviceTokenLifetimeDays);
         var tokenData = sut.NewDeviceToken(userId);

         Assert.IsNotNull(tokenData);
         Assert.IsNotEmpty(tokenData.Token);
         Assert.AreEqual(expectedTokenExpiration.Ticks, tokenData.Expiration.Ticks, TimeSpan.TicksPerSecond);

         var maybeClaimsPrincipal = sut.ValidateToken(tokenData.Token);
         maybeClaimsPrincipal.IfNone(Assert.Fail);
         maybeClaimsPrincipal.IfSome(claimsPrincipal =>
         {
            Assert.IsNotNull(claimsPrincipal);

            var parsedUserId = sut.ParseUserId(claimsPrincipal);
            Assert.AreEqual(userId, parsedUserId);

            var maybeTokenId = sut.TryParseTokenId(claimsPrincipal);
            maybeTokenId.IfNone(Assert.Fail);
         });
      }
   }
}
