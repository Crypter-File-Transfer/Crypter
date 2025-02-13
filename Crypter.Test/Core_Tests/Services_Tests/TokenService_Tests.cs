/*
 * Copyright (C) 2024 Crypter File Transfer
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
using System.Security.Claims;
using Crypter.Core.Identity;
using Crypter.Core.Services;
using EasyMonads;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Crypter.Test.Core_Tests.Services_Tests;

[TestFixture]
public class TokenService_Tests
{
    private IOptions<TokenSettings>? _tokenSettings;

    [SetUp]
    public void Setup()
    {
        TokenSettings tokenSettings = new TokenSettings
        {
            Audience = "test aud",
            Issuer = "test iss",
            AuthenticationTokenLifetimeMinutes = 5,
            SessionTokenLifetimeMinutes = 30,
            DeviceTokenLifetimeDays = 5
        };

        _tokenSettings = Options.Create(tokenSettings);
    }

    [Test]
    public void Can_Create_And_Validate_Authentication_Token()
    {
        Guid userId = Guid.NewGuid();

        TokenService sut = new TokenService(_tokenSettings!);
        string token = sut.NewAuthenticationToken(userId);

        Assert.That(token, Is.Not.Null);
        Assert.That(token, Is.Not.Empty);

        Maybe<ClaimsPrincipal> maybeClaimsPrincipal = sut.ValidateToken(token);
        maybeClaimsPrincipal.IfNone(Assert.Fail);
        maybeClaimsPrincipal.IfSome(x =>
        {
            Assert.That(x, Is.Not.Null);
            Guid parsedUserId = TokenService.ParseUserId(x);
            Assert.That(userId, Is.EqualTo(parsedUserId));
        });
    }

    [Test]
    public void Can_Create_And_Validate_Session_Token()
    {
        Guid userId = Guid.NewGuid();

        TokenService sut = new TokenService(_tokenSettings!);
        DateTime tokenCreatedUtc = DateTime.UtcNow;
        DateTime expectedTokenExpiration = tokenCreatedUtc.AddMinutes(_tokenSettings!.Value.SessionTokenLifetimeMinutes);
        RefreshTokenData tokenData = sut.NewSessionToken(userId);

        Assert.That(tokenData, Is.Not.Null);
        Assert.That(tokenData.Token, Is.Not.Empty);
        Assert.That(tokenData.Expiration.Ticks, Is.EqualTo(expectedTokenExpiration.Ticks).Within(TimeSpan.TicksPerSecond));

        Maybe<ClaimsPrincipal> maybeClaimsPrincipal = sut.ValidateToken(tokenData.Token);
        maybeClaimsPrincipal.IfNone(Assert.Fail);
        maybeClaimsPrincipal.IfSome(claimsPrincipal =>
        {
            Assert.That(claimsPrincipal, Is.Not.Null);

            Guid parsedUserId = TokenService.ParseUserId(claimsPrincipal);
            Assert.That(parsedUserId, Is.EqualTo(userId));

            Maybe<Guid> maybeTokenId = TokenService.TryParseTokenId(claimsPrincipal);
            maybeTokenId.IfNone(Assert.Fail);
        });
    }

    [Test]
    public void Can_Create_And_Validate_Device_Token()
    {
        Guid userId = Guid.NewGuid();

        TokenService sut = new TokenService(_tokenSettings!);
        DateTime tokenCreatedUtc = DateTime.UtcNow;
        DateTime expectedTokenExpiration = tokenCreatedUtc.AddDays(_tokenSettings!.Value.DeviceTokenLifetimeDays);
        RefreshTokenData tokenData = sut.NewDeviceToken(userId);

        Assert.That(tokenData, Is.Not.Null);
        Assert.That(tokenData.Token, Is.Not.Empty);
        Assert.That(tokenData.Expiration.Ticks, Is.EqualTo(expectedTokenExpiration.Ticks).Within(TimeSpan.TicksPerSecond));

        Maybe<ClaimsPrincipal> maybeClaimsPrincipal = sut.ValidateToken(tokenData.Token);
        maybeClaimsPrincipal.IfNone(Assert.Fail);
        maybeClaimsPrincipal.IfSome(claimsPrincipal =>
        {
            Assert.That(claimsPrincipal, Is.Not.Null);

            Guid parsedUserId = TokenService.ParseUserId(claimsPrincipal);
            Assert.That(parsedUserId, Is.EqualTo(userId));

            Maybe<Guid> maybeTokenId = TokenService.TryParseTokenId(claimsPrincipal);
            maybeTokenId.IfNone(Assert.Fail);
        });
    }
}
