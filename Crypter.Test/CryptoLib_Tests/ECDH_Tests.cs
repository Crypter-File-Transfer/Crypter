using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using NUnit.Framework;
using System.IO;

namespace Crypter.Test.CryptoLib_Tests
{
   public class ECDH_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void KeyGen_Works()
      {
         Assert.DoesNotThrow(() => ECDH.GenerateKeys());
      }

      [Test]
      public void KeyGen_Produces_Unique_Keys()
      {
         var alice = ECDH.GenerateKeys().Private.ConvertToPEM();
         var bob = ECDH.GenerateKeys().Private.ConvertToPEM();
         Assert.AreNotEqual(alice, bob);
      }

      [Test]
      public void Private_Key_Can_Be_Deserialized()
      {
         var alice = ECDH.GenerateKeys().Private;
         var pemPrivate = alice.ConvertToPEM();
         Assert.DoesNotThrow(() => KeyConversion.ConvertX25519PrivateKeyFromPEM(pemPrivate));

         var deserializedPrivate = KeyConversion.ConvertX25519PrivateKeyFromPEM(pemPrivate);
         Assert.AreEqual(pemPrivate, deserializedPrivate.ConvertToPEM());
      }

      [Test]
      public void Shared_Key_Derivation_Works()
      {
         var alice = ECDH.GenerateKeys();
         var bob = ECDH.GenerateKeys();
         Assert.DoesNotThrow(() => ECDH.DeriveSharedKey(alice.Private, bob.Public));
      }

      [Test]
      public void Known_Keys_Derive_The_Same_Shared_Key()
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var alicePemFile = Path.Combine(directory, "CryptoLib_Tests", "Assets", "Alice_ECDH_Key.pem");
         var bobPemFile = Path.Combine(directory, "CryptoLib_Tests", "Assets", "Bob_ECDH_Key.pem");

         var alicePemKey = File.ReadAllText(alicePemFile);
         var bobPemKey = File.ReadAllText(bobPemFile);

         var alicePrivate = KeyConversion.ConvertX25519PrivateKeyFromPEM(alicePemKey);
         var bobPrivate = KeyConversion.ConvertX25519PrivateKeyFromPEM(bobPemKey);

         var sharedKey = ECDH.DeriveSharedKey(alicePrivate, bobPrivate.GeneratePublicKey());

         var knownSharedKey = new byte[]
         {
            0xbf, 0x97, 0x79, 0xba, 0x97, 0xfa, 0x65, 0x6b,
            0x3d, 0xdd, 0x69, 0xcd, 0xcd, 0xf5, 0xa7, 0xa0,
            0x3d, 0xdf, 0x51, 0xc7, 0xc3, 0x13, 0x13, 0x3c,
            0x8f, 0x5a, 0x11, 0x21, 0x44, 0xbd, 0x0c, 0x3a
         };

         Assert.AreEqual(knownSharedKey, sharedKey);
      }

      [Test]
      public void Shared_Key_Derivation_Works_Both_Ways()
      {
         var alice = ECDH.GenerateKeys();
         var bob = ECDH.GenerateKeys();

         var sharedKey1 = ECDH.DeriveSharedKey(alice.Private, bob.Public);
         var sharedKey2 = ECDH.DeriveSharedKey(bob.Private, alice.Public);
         Assert.AreEqual(sharedKey1, sharedKey2);
      }

      [Test]
      public void Shared_Key_Derivation_Produces_Unique_Keys()
      {
         var alice = ECDH.GenerateKeys();
         var bob = ECDH.GenerateKeys();
         var charlie = ECDH.GenerateKeys();

         var aliceBob = ECDH.DeriveSharedKey(alice.Private, bob.Public);
         var bobCharlie = ECDH.DeriveSharedKey(bob.Private, charlie.Public);
         var charlieAlice = ECDH.DeriveSharedKey(charlie.Private, alice.Public);
         Assert.AreNotEqual(aliceBob, bobCharlie);
         Assert.AreNotEqual(bobCharlie, charlieAlice);
         Assert.AreNotEqual(charlieAlice, aliceBob);
      }

      [Test]
      public void Receive_And_Send_Key_Derivation_Works_Both_Ways()
      {
         var alice = ECDH.GenerateKeys();
         var bob = ECDH.GenerateKeys();

         (var aliceReceive, var aliceSend) = ECDH.DeriveSharedKeys(alice, bob.Public);
         (var bobReceive, var bobSend) = ECDH.DeriveSharedKeys(bob, alice.Public);
         Assert.AreEqual(aliceReceive, bobSend);
         Assert.AreEqual(aliceSend, bobReceive);
      }
   }
}
