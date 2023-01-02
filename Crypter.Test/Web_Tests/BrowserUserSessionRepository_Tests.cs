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

using Crypter.ClientServices.DeviceStorage.Enums;
using Crypter.ClientServices.DeviceStorage.Models;
using Crypter.ClientServices.Interfaces.Repositories;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Web.Repositories;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Crypter.Test.Web_Tests
{
   [TestFixture]
   public class BrowserUserSessionRepository_Tests
   {
      private Mock<IDeviceRepository<BrowserStorageLocation>> _browserStorageMock;

      [SetUp]
      public void Setup()
      {
         _browserStorageMock = new Mock<IDeviceRepository<BrowserStorageLocation>>();
      }

      [Test]
      public async Task Repository_Stores_User_Session_In_Session_Storage()
      {
         _browserStorageMock
            .Setup(x => x.SetItemAsync(
               DeviceStorageObjectType.UserSession,
               It.IsAny<UserSession>(),
               BrowserStorageLocation.SessionStorage))
            .ReturnsAsync((DeviceStorageObjectType objectType, UserSession userSession, BrowserStorageLocation browserStorageLocation) => Unit.Default);

         var sut = new BrowserUserSessionRepository(_browserStorageMock.Object);

         UserSession session = new(Username.From("foo"), false, UserSession.LATEST_SCHEMA);
         await sut.StoreUserSessionAsync(session, false);

         _browserStorageMock.Verify(x => x.SetItemAsync(DeviceStorageObjectType.UserSession, It.IsAny<UserSession>(), BrowserStorageLocation.SessionStorage), Times.Once);
      }

      [Test]
      public async Task Repository_Stores_User_Session_In_Local_Storage()
      {
         _browserStorageMock
            .Setup(x => x.SetItemAsync(
               DeviceStorageObjectType.UserSession,
               It.IsAny<UserSession>(),
               BrowserStorageLocation.LocalStorage))
            .ReturnsAsync((DeviceStorageObjectType objectType, UserSession userSession, BrowserStorageLocation browserStorageLocation) => Unit.Default);

         var sut = new BrowserUserSessionRepository(_browserStorageMock.Object);

         UserSession session = new(Username.From("foo"), true, UserSession.LATEST_SCHEMA);
         await sut.StoreUserSessionAsync(session, true);

         _browserStorageMock.Verify(x => x.SetItemAsync(DeviceStorageObjectType.UserSession, It.IsAny<UserSession>(), BrowserStorageLocation.LocalStorage), Times.Once);
      }

      [Test]
      public async Task Repository_Gets_Some_User_Session()
      {
         _browserStorageMock
            .Setup(x => x.GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession))
            .ReturnsAsync((DeviceStorageObjectType objectType) => new UserSession("foo", true, UserSession.LATEST_SCHEMA));

         var sut = new BrowserUserSessionRepository(_browserStorageMock.Object);

         var fetchedSession = await sut.GetUserSessionAsync();
         fetchedSession.IfNone(Assert.Fail);
         fetchedSession.IfSome(x =>
         {
            Assert.AreEqual("foo", x.Username);
            Assert.IsTrue(x.RememberUser);
         });

         _browserStorageMock.Verify(x => x.GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession), Times.Once);
      }

      [Test]
      public async Task Repository_Gets_None_User_Session()
      {
         _browserStorageMock
            .Setup(x => x.GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession))
            .ReturnsAsync((DeviceStorageObjectType objectType) => Maybe<UserSession>.None);

         var sut = new BrowserUserSessionRepository(_browserStorageMock.Object);

         var fetchedSession = await sut.GetUserSessionAsync();
         Assert.IsTrue(fetchedSession.IsNone);

         _browserStorageMock.Verify(x => x.GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession), Times.Once);
      }
   }
}
