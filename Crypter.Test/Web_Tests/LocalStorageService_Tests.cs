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
         var storedUserSession = new UserSession(Guid.NewGuid(), "foo", "1234", "5678");
         var storedAuthToken = "authToken";
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

         // AuthToken
         jsRuntime
            .Setup(x => x.InvokeAsync<string>(
               It.Is<string>(x => x == $"{storageLiteral}.getItem"),
               It.Is<object[]>(x => x[0].ToString() == StoredObjectType.AuthToken.ToString())))
            .ReturnsAsync((string command, object[] args) => JsonConvert.SerializeObject(storedAuthToken));

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
         Assert.AreEqual(storedUserSession.EncryptedAuthToken, fetchedUserSession.EncryptedAuthToken);
         Assert.AreEqual(storedUserSession.AuthTokenIV, fetchedUserSession.AuthTokenIV);

         var fetchedAuthToken = await sut.GetItemAsync<string>(StoredObjectType.AuthToken);
         Assert.AreEqual(storedAuthToken, fetchedAuthToken);

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
