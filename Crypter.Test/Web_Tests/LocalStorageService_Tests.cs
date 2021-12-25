/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.Web.Models.LocalStorage;
using Crypter.Web.Services;
using Microsoft.JSInterop;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Crypter.Test.Web_Tests
{
   [TestFixture]
   public class LocalStorageService_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public async Task Service_Properly_Identifies_As_Initialized()
      {
      var jsRuntime = new Mock<IJSRuntime>();
      jsRuntime
         .Setup(x => x.InvokeAsync<string>(It.IsAny<string>(), new object[] { It.IsAny<string>() }))
         .ReturnsAsync((string command, string itemType) => default);

         var sut = new LocalStorageService(jsRuntime.Object);

         Assert.IsFalse(sut.IsInitialized);
         await sut.InitializeAsync();
         Assert.IsTrue(sut.IsInitialized);
      }

      [Test]
      public async Task Storage_Is_Empty_Upon_Initialization_Without_Existing_Data()
      {
         var jsRuntime = new Mock<IJSRuntime>();
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(It.IsAny<string>(), new object[] { It.IsAny<string>() }))
            .ReturnsAsync((string command, string itemType) => default);

         var sut = new LocalStorageService(jsRuntime.Object);
         await sut.InitializeAsync();

         foreach (StoredObjectType item in Enum.GetValues(typeof(StoredObjectType)))
         {
            Assert.IsFalse(sut.HasItem(item));
         }
      }

      [TestCase(LocalStorageService.SessionStorageLiteral, StorageLocation.SessionStorage)]
      [TestCase(LocalStorageService.LocalStorageLiteral, StorageLocation.LocalStorage)]
      public async Task Service_Initializes_Values_From_Browser_Storage(string storageLiteral, StorageLocation storageLocation)
      {
         var storedUserSession = new UserSession(Guid.NewGuid(), "foo", "refresh");
         var authenticationToken = "jwt";
         var plaintextX25519PrivateKey = "plaintextX25519";
         var plaintextEd25519PrivateKey = "plaintextEd25519";
         var encryptedX25519PrivateKey = "encryptedX25519";
         var encryptedEd25519PrivateKey = "encryptedEd25519";

         var jsRuntime = new Mock<IJSRuntime>();

         // UserSession
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == StoredObjectType.UserSession.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(storedUserSession));

         // AuthenticationToken
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == StoredObjectType.AuthenticationToken.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(authenticationToken));

         // PlaintextX25519PrivateKey
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == StoredObjectType.PlaintextX25519PrivateKey.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(plaintextX25519PrivateKey));

         // PlaintextEd25519PrivateKey
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == StoredObjectType.PlaintextEd25519PrivateKey.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(plaintextEd25519PrivateKey));

         // EncryptedX25519PrivateKey
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == StoredObjectType.EncryptedX25519PrivateKey.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(encryptedX25519PrivateKey));

         // EncryptedEd25519PrivateKey
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == StoredObjectType.EncryptedEd25519PrivateKey.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(encryptedEd25519PrivateKey));

         var sut = new LocalStorageService(jsRuntime.Object);
         await sut.InitializeAsync();

         foreach (StoredObjectType item in Enum.GetValues(typeof(StoredObjectType)))
         {
            Assert.IsTrue(sut.HasItem(item));
            Assert.AreEqual(storageLocation, sut.GetItemLocation(item));
         }

         var fetchedUserSession = await sut.GetItemAsync<UserSession>(StoredObjectType.UserSession);
         Assert.AreEqual(storedUserSession.UserId, fetchedUserSession.UserId);
         Assert.AreEqual(storedUserSession.Username, fetchedUserSession.Username);
         Assert.AreEqual(storedUserSession.RefreshToken, fetchedUserSession.RefreshToken);

         var fetchedAuthenticationToken = await sut.GetItemAsync<string>(StoredObjectType.AuthenticationToken);
         Assert.AreEqual(authenticationToken, fetchedAuthenticationToken);

         var fetchedPlaintextX25519PrivateKey = await sut.GetItemAsync<string>(StoredObjectType.PlaintextX25519PrivateKey);
         Assert.AreEqual(plaintextX25519PrivateKey, fetchedPlaintextX25519PrivateKey);

         var fetchedPlaintextEd25519PrivateKey = await sut.GetItemAsync<string>(StoredObjectType.PlaintextEd25519PrivateKey);
         Assert.AreEqual(plaintextEd25519PrivateKey, fetchedPlaintextEd25519PrivateKey);

         var fetchedEncryptedX25519PrivateKey = await sut.GetItemAsync<string>(StoredObjectType.EncryptedX25519PrivateKey);
         Assert.AreEqual(encryptedX25519PrivateKey, fetchedEncryptedX25519PrivateKey);

         var fetchedEncryptedEd25519PrivateKey = await sut.GetItemAsync<string>(StoredObjectType.EncryptedEd25519PrivateKey);
         Assert.AreEqual(encryptedEd25519PrivateKey, fetchedEncryptedEd25519PrivateKey);
      }
   }
}
