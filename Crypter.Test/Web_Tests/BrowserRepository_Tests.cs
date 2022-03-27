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

using Crypter.ClientServices.DeviceStorage.Enums;
using Crypter.ClientServices.DeviceStorage.Models;
using Crypter.Web.Repositories;
using Microsoft.JSInterop;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Crypter.Test.Web_Tests
{
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
         var jsRuntime = new Mock<IJSRuntime>();
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(It.IsAny<string>(), new object[] { It.IsAny<string>() }))
            .ReturnsAsync((string command, string itemType) => default);

         var sut = new BrowserRepository(jsRuntime.Object);
         await sut.InitializeAsync();

         foreach (DeviceStorageObjectType item in Enum.GetValues(typeof(DeviceStorageObjectType)))
         {
            Assert.IsFalse(sut.HasItem(item));
         }
      }

      [TestCase(BrowserRepository.SessionStorageLiteral, BrowserStorageLocation.SessionStorage)]
      [TestCase(BrowserRepository.LocalStorageLiteral, BrowserStorageLocation.LocalStorage)]
      public async Task Repository_Initializes_Values_From_Browser_Storage(string storageLiteral, BrowserStorageLocation storageLocation)
      {
         var storedUserSession = new UserSession("foo", true);
         var authenticationToken = "authentication";
         var refreshToken = "refresh";
         var x25519PrivateKey = "plaintextX25519";
         var ed25519PrivateKey = "plaintextEd25519";
         var jsRuntime = new Mock<IJSRuntime>();

         // UserSession
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == DeviceStorageObjectType.UserSession.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(storedUserSession));

         // AuthenticationToken
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == DeviceStorageObjectType.AuthenticationToken.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(authenticationToken));

         // RefreshToken
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == DeviceStorageObjectType.RefreshToken.ToString())))
            .ReturnsAsync((string commands, object[] args) => JsonConvert.SerializeObject(refreshToken));

         // Ed25519PrivateKey
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == DeviceStorageObjectType.Ed25519PrivateKey.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(ed25519PrivateKey));

         // X25519PrivateKey
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == DeviceStorageObjectType.X25519PrivateKey.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(x25519PrivateKey));

         var sut = new BrowserRepository(jsRuntime.Object);
         await sut.InitializeAsync();

         foreach (DeviceStorageObjectType item in Enum.GetValues(typeof(DeviceStorageObjectType)))
         {
            Assert.IsTrue(sut.HasItem(item));
            Assert.AreEqual(storageLocation, sut.GetItemLocation(item).SomeOrDefault());
         }

         var fetchedUserSession = await sut.GetItemAsync<UserSession>(DeviceStorageObjectType.UserSession);
         Assert.AreEqual(storedUserSession.Username, fetchedUserSession.SomeOrDefault().Username);

         var fetchedAuthenticationToken = await sut.GetItemAsync<string>(DeviceStorageObjectType.AuthenticationToken);
         Assert.AreEqual(authenticationToken, fetchedAuthenticationToken.SomeOrDefault());

         var fetchedRefreshToken = await sut.GetItemAsync<string>(DeviceStorageObjectType.RefreshToken);
         Assert.AreEqual(refreshToken, fetchedRefreshToken.SomeOrDefault());

         var fetchedPlaintextEd25519PrivateKey = await sut.GetItemAsync<string>(DeviceStorageObjectType.Ed25519PrivateKey);
         Assert.AreEqual(ed25519PrivateKey, fetchedPlaintextEd25519PrivateKey.SomeOrDefault());

         var fetchedPlaintextX25519PrivateKey = await sut.GetItemAsync<string>(DeviceStorageObjectType.X25519PrivateKey);
         Assert.AreEqual(x25519PrivateKey, fetchedPlaintextX25519PrivateKey.SomeOrDefault());
      }
   }
}
