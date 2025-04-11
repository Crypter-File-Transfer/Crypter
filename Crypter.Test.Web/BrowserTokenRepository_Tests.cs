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

using System.Threading.Tasks;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Models;
using Crypter.Common.Enums;
using Crypter.Web.Repositories;
using EasyMonads;
using NSubstitute;
using NUnit.Framework;

namespace Crypter.Test.Web;

[TestFixture]
public class BrowserTokenRepository_Tests
{
    private IDeviceRepository<BrowserStorageLocation>? _browserStorage;

    [SetUp]
    public void Setup()
    {
        _browserStorage = Substitute.For<IDeviceRepository<BrowserStorageLocation>>();
    }

    [Test]
    public async Task Repository_Stores_Authentication_Token_As_Token_Object()
    {
        _browserStorage!
            .SetItemAsync(
                DeviceStorageObjectType.AuthenticationToken,
                Arg.Any<TokenObject>(),
                BrowserStorageLocation.Memory)
            .Returns(Task.FromResult(Unit.Default));

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorage!);
        await sut.StoreAuthenticationTokenAsync("foo");

        await _browserStorage!.Received(1)
            .SetItemAsync(
                DeviceStorageObjectType.AuthenticationToken,
                Arg.Any<TokenObject>(),
                BrowserStorageLocation.Memory);
    }

    [TestCase(TokenType.Session, BrowserStorageLocation.SessionStorage)]
    [TestCase(TokenType.Device, BrowserStorageLocation.LocalStorage)]
    public async Task Repository_Stores_Refresh_Token_As_Token_Object(TokenType tokenType,
        BrowserStorageLocation browserStorageLocation)
    {
        _browserStorage!
            .SetItemAsync(
                DeviceStorageObjectType.RefreshToken,
                Arg.Any<TokenObject>(),
                browserStorageLocation)
            .Returns(Task.FromResult(Unit.Default));

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorage!);
        await sut.StoreRefreshTokenAsync("foo", tokenType);

        await _browserStorage!.Received(1)
            .SetItemAsync(
                DeviceStorageObjectType.RefreshToken,
                Arg.Any<TokenObject>(),
                browserStorageLocation);
    }

    [Test]
    public async Task Repository_Gets_Some_Authentication_Token()
    {
        _browserStorage!
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken)
            .Returns(Task.FromResult<Maybe<TokenObject>>(new TokenObject(TokenType.Authentication, "foo")));
        
        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorage!);

        Maybe<TokenObject> fetchedToken = await sut.GetAuthenticationTokenAsync();
        fetchedToken.IfNone(Assert.Fail);
        fetchedToken.IfSome(x =>
        {
            Assert.That(x.TokenType, Is.EqualTo(TokenType.Authentication));
            Assert.That(x.Token, Is.EqualTo("foo"));
        });

        await _browserStorage!.Received(1)
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken);
    }

    [Test]
    public async Task Repository_Gets_None_Authentication_Token()
    {
        _browserStorage!
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken)
            .Returns(Task.FromResult(Maybe<TokenObject>.None));

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorage!);

        Maybe<TokenObject> fetchedToken = await sut.GetAuthenticationTokenAsync();
        Assert.That(fetchedToken.IsNone, Is.True);

        await _browserStorage!.Received(1)
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken);
    }

    [TestCase(TokenType.Session)]
    [TestCase(TokenType.Device)]
    public async Task Repository_Gets_Some_Refresh_Token(TokenType tokenType)
    {
        _browserStorage!
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken)
            .Returns(Task.FromResult<Maybe<TokenObject>>(new TokenObject(tokenType, "foo")));

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorage!);

        Maybe<TokenObject> fetchedToken = await sut.GetRefreshTokenAsync();
        fetchedToken.IfNone(Assert.Fail);
        fetchedToken.IfSome(x =>
        {
            Assert.That(x.TokenType, Is.EqualTo(tokenType));
            Assert.That(x.Token, Is.EqualTo("foo"));
        });

        await _browserStorage!.Received(1)
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken);
    }

    [Test]
    public async Task Repository_Gets_None_Refresh_Token()
    {
        _browserStorage!
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken)
            .Returns(Task.FromResult(Maybe<TokenObject>.None));

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorage!);

        Maybe<TokenObject> fetchedToken = await sut.GetRefreshTokenAsync();
        Assert.That(fetchedToken.IsNone, Is.True);

        await _browserStorage!.Received(1)
            .GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken);
    }
}
