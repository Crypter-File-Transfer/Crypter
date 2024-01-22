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
using System.Text.Json;
using System.Threading.Tasks;
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Models;
using Crypter.Web.Repositories;
using EasyMonads;
using Microsoft.JSInterop;
using Moq;
using NUnit.Framework;

namespace Crypter.Test.Web_Tests;

[TestFixture]
public class BrowserRepository_Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Storage_Is_Empty_Upon_Initialization_Without_Existing_Data()
    {
        Mock<IJSRuntime> jsRuntime = new Mock<IJSRuntime>();
        jsRuntime
            .Setup(x => x.InvokeAsync<string?>(It.IsAny<string>(), new object[] { It.IsAny<string>() }))
            .ReturnsAsync((string _, string _) => null);

        BrowserRepository sut = new BrowserRepository(jsRuntime.Object);
        await sut.InitializeAsync();

        foreach (DeviceStorageObjectType item in Enum.GetValues(typeof(DeviceStorageObjectType)))
        {
            Assert.That(sut.HasItem(item), Is.False);
        }
    }

    [TestCase(BrowserRepository.SessionStorageLiteral, BrowserStorageLocation.SessionStorage)]
    [TestCase(BrowserRepository.LocalStorageLiteral, BrowserStorageLocation.LocalStorage)]
    public async Task Repository_Initializes_Values_From_Browser_Storage(string storageLiteral,
        BrowserStorageLocation storageLocation)
    {
        UserSession storedUserSession = new UserSession("foo", true, UserSession.LATEST_SCHEMA);
        const string authenticationToken = "authentication";
        const string refreshToken = "refresh";
        byte[] privateKey = "privateKey"u8.ToArray();
        byte[] masterKey = "masterKey"u8.ToArray();
        Mock<IJSRuntime> jsRuntime = new Mock<IJSRuntime>();

        // UserSession
        jsRuntime
            .Setup(x => x.InvokeAsync<string>(
                It.Is<string>(y => y == $"{storageLiteral}.getItem"),
                It.Is<object[]>(y => y[0].ToString() == DeviceStorageObjectType.UserSession.ToString())))
            .ReturnsAsync((string _, object[] _) => JsonSerializer.Serialize(storedUserSession));

        // AuthenticationToken
        jsRuntime
            .Setup(x => x.InvokeAsync<string>(
                It.Is<string>(y => y == $"{storageLiteral}.getItem"),
                It.Is<object[]>(y => y[0].ToString() == DeviceStorageObjectType.AuthenticationToken.ToString())))
            .ReturnsAsync((string _, object[] _) => JsonSerializer.Serialize(authenticationToken));

        // RefreshToken
        jsRuntime
            .Setup(x => x.InvokeAsync<string>(
                It.Is<string>(y => y == $"{storageLiteral}.getItem"),
                It.Is<object[]>(y => y[0].ToString() == DeviceStorageObjectType.RefreshToken.ToString())))
            .ReturnsAsync((string _, object[] _) => JsonSerializer.Serialize(refreshToken));

        // PrivateKey
        jsRuntime
            .Setup(x => x.InvokeAsync<string>(
                It.Is<string>(y => y == $"{storageLiteral}.getItem"),
                It.Is<object[]>(y => y[0].ToString() == DeviceStorageObjectType.PrivateKey.ToString())))
            .ReturnsAsync((string _, object[] _) => JsonSerializer.Serialize(privateKey));

        // MasterKey
        jsRuntime
            .Setup(x => x.InvokeAsync<string>(
                It.Is<string>(y => y == $"{storageLiteral}.getItem"),
                It.Is<object[]>(y => y[0].ToString() == DeviceStorageObjectType.MasterKey.ToString())))
            .ReturnsAsync((string _, object[] _) => JsonSerializer.Serialize(masterKey));

        BrowserRepository sut = new BrowserRepository(jsRuntime.Object);
        await sut.InitializeAsync();

        foreach (DeviceStorageObjectType item in Enum.GetValues(typeof(DeviceStorageObjectType)))
        {
            Assert.That(sut.HasItem(item), Is.True);
            Maybe<BrowserStorageLocation> itemLocation = sut.GetItemLocation(item);

            itemLocation.IfNone(Assert.Fail);
            itemLocation.IfSome(x => Assert.That(x, Is.EqualTo(storageLocation)));
        }

        Maybe<UserSession> fetchedUserSession = await sut.GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession);
        fetchedUserSession.IfNone(Assert.Fail);
        fetchedUserSession.IfSome(x => Assert.That(x.Username, Is.EqualTo(storedUserSession.Username)));

        Maybe<string> fetchedAuthenticationToken = await sut.GetItemAsync<string>(DeviceStorageObjectType.AuthenticationToken);
        fetchedAuthenticationToken.IfNone(Assert.Fail);
        fetchedAuthenticationToken.IfSome(x => Assert.That(x, Is.EqualTo(authenticationToken)));

        Maybe<string> fetchedRefreshToken = await sut.GetItemAsync<string>(DeviceStorageObjectType.RefreshToken);
        fetchedRefreshToken.IfNone(Assert.Fail);
        fetchedRefreshToken.IfSome(x => Assert.That(x, Is.EqualTo(refreshToken)));

        Maybe<byte[]> fetchedPrivateKey = await sut.GetItemAsync<byte[]>(DeviceStorageObjectType.PrivateKey);
        fetchedPrivateKey.IfNone(Assert.Fail);
        fetchedPrivateKey.IfSome(x => Assert.That(x, Is.EqualTo(privateKey)));

        Maybe<byte[]> fetchedMasterKeyKey = await sut.GetItemAsync<byte[]>(DeviceStorageObjectType.MasterKey);
        fetchedMasterKeyKey.IfNone(Assert.Fail);
        fetchedMasterKeyKey.IfSome(x => Assert.That(x, Is.EqualTo(masterKey)));
    }
}
