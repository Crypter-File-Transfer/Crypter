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
using Crypter.Common.Primitives;
using Crypter.Web.Repositories;
using EasyMonads;
using NSubstitute;
using NUnit.Framework;

namespace Crypter.Test.Web;

[TestFixture]
public class BrowserUserSessionRepository_Tests
{
    private IDeviceRepository<BrowserStorageLocation>? _browserStorage;


    [SetUp]
    public void Setup()
    {
        _browserStorage = Substitute.For<IDeviceRepository<BrowserStorageLocation>>();
    }

    [Test]
    public async Task Repository_Stores_User_Session_In_Session_Storage()
    {
        _browserStorage!
            .SetItemAsync(
                DeviceStorageObjectType.UserSession,
                Arg.Any<UserSession>(),
                BrowserStorageLocation.SessionStorage)
            .Returns(Task.FromResult(Unit.Default));

        BrowserUserSessionRepository sut = new BrowserUserSessionRepository(_browserStorage!);

        UserSession session = new UserSession(Username.From("foo"), false, UserSession.LATEST_SCHEMA);
        await sut.StoreUserSessionAsync(session, false);

        await _browserStorage!.Received(1)
            .SetItemAsync(
                DeviceStorageObjectType.UserSession,
                Arg.Any<UserSession>(),
                BrowserStorageLocation.SessionStorage);
    }

    [Test]
    public async Task Repository_Stores_User_Session_In_Local_Storage()
    {
        _browserStorage!
            .SetItemAsync(
                DeviceStorageObjectType.UserSession,
                Arg.Any<UserSession>(),
                BrowserStorageLocation.LocalStorage)
            .Returns(Task.FromResult(Unit.Default));

        BrowserUserSessionRepository sut = new BrowserUserSessionRepository(_browserStorage!);

        UserSession session = new UserSession(Username.From("foo"), true, UserSession.LATEST_SCHEMA);
        await sut.StoreUserSessionAsync(session, true);

        await _browserStorage!.Received(1)
            .SetItemAsync(
                DeviceStorageObjectType.UserSession,
                Arg.Any<UserSession>(),
                BrowserStorageLocation.LocalStorage);
    }

    [Test]
    public async Task Repository_Gets_Some_User_Session()
    {
        _browserStorage!
            .GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession)
            .Returns(Task.FromResult<Maybe<UserSession>>(new UserSession("foo", true, UserSession.LATEST_SCHEMA)));

        BrowserUserSessionRepository sut = new BrowserUserSessionRepository(_browserStorage!);

        Maybe<UserSession> fetchedSession = await sut.GetUserSessionAsync();
        fetchedSession.IfNone(Assert.Fail);
        fetchedSession.IfSome(x =>
        {
            Assert.That(x.Username, Is.EqualTo("foo"));
            Assert.That(x.RememberUser, Is.True);
        });

        await _browserStorage!.Received(1)
            .GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession);
    }

    [Test]
    public async Task Repository_Gets_None_User_Session()
    {
        _browserStorage!
            .GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession)
            .Returns(Task.FromResult(Maybe<UserSession>.None));

        BrowserUserSessionRepository sut = new BrowserUserSessionRepository(_browserStorage!);

        Maybe<UserSession> fetchedSession = await sut.GetUserSessionAsync();
        Assert.That(fetchedSession.IsNone, Is.True);

        await _browserStorage!.Received(1)
            .GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession);
    }
}
