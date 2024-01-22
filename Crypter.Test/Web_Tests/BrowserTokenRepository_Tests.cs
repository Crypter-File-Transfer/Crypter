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

using System.Threading.Tasks;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Interfaces.Repositories;
using Crypter.Common.Client.Models;
using Crypter.Common.Enums;
using Crypter.Web.Repositories;
using EasyMonads;
using Moq;
using NUnit.Framework;

namespace Crypter.Test.Web_Tests;

[TestFixture]
public class BrowserTokenRepository_Tests
{
    private Mock<IDeviceRepository<BrowserStorageLocation>>? _browserStorageMock;

    [SetUp]
    public void Setup()
    {
        _browserStorageMock = new Mock<IDeviceRepository<BrowserStorageLocation>>();
    }


    [Test]
    public async Task Repository_Stores_Authentication_Token_As_Token_Object()
    {
        _browserStorageMock!
            .Setup(x => x.SetItemAsync(
                DeviceStorageObjectType.AuthenticationToken,
                It.IsAny<TokenObject>(),
                BrowserStorageLocation.Memory))
            .ReturnsAsync((DeviceStorageObjectType _, TokenObject _, BrowserStorageLocation _) => Unit.Default);

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorageMock!.Object);
        await sut.StoreAuthenticationTokenAsync("foo");

        _browserStorageMock!.Verify(
            x => x.SetItemAsync(DeviceStorageObjectType.AuthenticationToken, It.IsAny<TokenObject>(),
                BrowserStorageLocation.Memory), Times.Once);
    }

    [TestCase(TokenType.Session, BrowserStorageLocation.SessionStorage)]
    [TestCase(TokenType.Device, BrowserStorageLocation.LocalStorage)]
    public async Task Repository_Stores_Refresh_Token_As_Token_Object(TokenType tokenType,
        BrowserStorageLocation browserStorageLocation)
    {
        _browserStorageMock!
            .Setup(x => x.SetItemAsync(
                DeviceStorageObjectType.RefreshToken,
                It.IsAny<TokenObject>(),
                browserStorageLocation))
            .ReturnsAsync(
                (DeviceStorageObjectType _, TokenObject _, BrowserStorageLocation _) => Unit.Default);

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorageMock!.Object);
        await sut.StoreRefreshTokenAsync("foo", tokenType);

        _browserStorageMock!.Verify(
            x => x.SetItemAsync(DeviceStorageObjectType.RefreshToken, It.IsAny<TokenObject>(), browserStorageLocation),
            Times.Once);
    }

    [Test]
    public async Task Repository_Gets_Some_Authentication_Token()
    {
        _browserStorageMock!
            .Setup(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken))
            .ReturnsAsync((DeviceStorageObjectType _) => new TokenObject(TokenType.Authentication, "foo"));

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorageMock!.Object);

        Maybe<TokenObject> fetchedToken = await sut.GetAuthenticationTokenAsync();
        fetchedToken.IfNone(Assert.Fail);
        fetchedToken.IfSome(x =>
        {
            Assert.That(x.TokenType, Is.EqualTo(TokenType.Authentication));
            Assert.That(x.Token, Is.EqualTo("foo"));
        });

        _browserStorageMock!.Verify(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken),
            Times.Once);
    }

    [Test]
    public async Task Repository_Gets_None_Authentication_Token()
    {
        _browserStorageMock!
            .Setup(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken))
            .ReturnsAsync((DeviceStorageObjectType _) => Maybe<TokenObject>.None);

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorageMock!.Object);

        Maybe<TokenObject> fetchedToken = await sut.GetAuthenticationTokenAsync();
        Assert.That(fetchedToken.IsNone, Is.True);

        _browserStorageMock!.Verify(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.AuthenticationToken),
            Times.Once);
    }

    [TestCase(TokenType.Session)]
    [TestCase(TokenType.Device)]
    public async Task Repository_Gets_Some_Refresh_Token(TokenType tokenType)
    {
        _browserStorageMock!
            .Setup(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken))
            .ReturnsAsync((DeviceStorageObjectType _) => new TokenObject(tokenType, "foo"));

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorageMock!.Object);

        Maybe<TokenObject> fetchedToken = await sut.GetRefreshTokenAsync();
        fetchedToken.IfNone(Assert.Fail);
        fetchedToken.IfSome(x =>
        {
            Assert.That(x.TokenType, Is.EqualTo(tokenType));
            Assert.That(x.Token, Is.EqualTo("foo"));
        });

        _browserStorageMock!.Verify(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken), Times.Once);
    }

    public async Task Repository_Gets_None_Refresh_Token()
    {
        _browserStorageMock!
            .Setup(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken))
            .ReturnsAsync((DeviceStorageObjectType _) => Maybe<TokenObject>.None);

        BrowserTokenRepository sut = new BrowserTokenRepository(_browserStorageMock!.Object);

        Maybe<TokenObject> fetchedToken = await sut.GetRefreshTokenAsync();
        Assert.That(fetchedToken.IsNone, Is.True);

        _browserStorageMock!.Verify(x => x.GetItemAsync<TokenObject>(DeviceStorageObjectType.RefreshToken), Times.Once);
    }
}
