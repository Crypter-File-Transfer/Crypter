using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using NUnit.Framework;

namespace Crypter.Test.CryptoLib_Tests
{
   public class ECDSA_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void KeyGen_Works()
      {
         Assert.DoesNotThrow(() => ECDSA.GenerateKeys());
      }

      [Test]
      public void KeyGen_Produces_Unique_Keys()
      {
         var alice = ECDSA.GenerateKeys().Private.ConvertToPEM();
         var bob = ECDSA.GenerateKeys().Private.ConvertToPEM();
         Assert.AreNotEqual(alice, bob);
      }

      [Test]
      public void Private_Key_Can_Be_Deserialized()
      {
         var alice = ECDSA.GenerateKeys().Private;
         var pemPrivate = alice.ConvertToPEM();
         Assert.DoesNotThrow(() => KeyConversion.ConvertEd25519PrivateKeyFromPEM(pemPrivate));

         var deserializedPrivate = KeyConversion.ConvertEd25519PrivateKeyFromPEM(pemPrivate);
         Assert.AreEqual(pemPrivate, deserializedPrivate.ConvertToPEM());
      }

      [Test]
      public void Signing_Is_Predictable()
      {
         var knownPrivateKeyPEM = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VwBCIEIMFjaUZrHJYPJH4O2bPTsnFwqXsGTVRooB2jw78TnGjH
gSEARRpYb3MlC/w8giB4NsNrKvPsnfuVsXBlHFywuEfJQQo=
-----END PRIVATE KEY-----";

         var knownPrivateKey = KeyConversion.ConvertEd25519PrivateKeyFromPEM(knownPrivateKeyPEM);

         var knownPlaintext = new byte[]
         {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10
         };

         var knownSignature = new byte[]
         {
            0xee, 0x14, 0xe8, 0x70, 0x22, 0x5f, 0xce, 0x8f,
            0xa4, 0x02, 0x39, 0x64, 0xcb, 0x77, 0x70, 0xdf,
            0x5b, 0x50, 0x22, 0x72, 0x92, 0x7e, 0xc6, 0x24,
            0xd6, 0x0d, 0xc9, 0x7d, 0xd7, 0x51, 0xbc, 0xe7,
            0xbc, 0x3b, 0x20, 0x79, 0x68, 0x6b, 0xf6, 0x2c,
            0x5f, 0x0c, 0x8e, 0xe5, 0xe3, 0xe4, 0x6b, 0xc9,
            0xa7, 0xd3, 0x10, 0xe0, 0x85, 0xea, 0x37, 0x9e,
            0x35, 0x0d, 0x80, 0xef, 0xf2, 0xef, 0xf9, 0x00
         };

         var signer = new ECDSA();
         signer.InitializeSigner(knownPrivateKey);
         signer.SignerDigestChunk(knownPlaintext);
         var signature = signer.GenerateSignature();

         Assert.AreEqual(knownSignature, signature);
      }

      [Test]
      public void Verification_Can_Succeed()
      {
         var knownPrivateKeyPEM = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VwBCIEIMFjaUZrHJYPJH4O2bPTsnFwqXsGTVRooB2jw78TnGjH
gSEARRpYb3MlC/w8giB4NsNrKvPsnfuVsXBlHFywuEfJQQo=
-----END PRIVATE KEY-----";

         var knownPrivateKey = KeyConversion.ConvertEd25519PrivateKeyFromPEM(knownPrivateKeyPEM);
         var knownPublicKey = knownPrivateKey.GeneratePublicKey();

         var knownPlaintext = new byte[]
         {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10
         };

         var knownSignature = new byte[]
         {
            0xee, 0x14, 0xe8, 0x70, 0x22, 0x5f, 0xce, 0x8f,
            0xa4, 0x02, 0x39, 0x64, 0xcb, 0x77, 0x70, 0xdf,
            0x5b, 0x50, 0x22, 0x72, 0x92, 0x7e, 0xc6, 0x24,
            0xd6, 0x0d, 0xc9, 0x7d, 0xd7, 0x51, 0xbc, 0xe7,
            0xbc, 0x3b, 0x20, 0x79, 0x68, 0x6b, 0xf6, 0x2c,
            0x5f, 0x0c, 0x8e, 0xe5, 0xe3, 0xe4, 0x6b, 0xc9,
            0xa7, 0xd3, 0x10, 0xe0, 0x85, 0xea, 0x37, 0x9e,
            0x35, 0x0d, 0x80, 0xef, 0xf2, 0xef, 0xf9, 0x00
         };

         var verifier = new ECDSA();
         verifier.InitializeVerifier(knownPublicKey);
         verifier.VerifierDigestChunk(knownPlaintext);
         var verificationResult = verifier.VerifySignature(knownSignature);

         Assert.True(verificationResult);
      }

      [Test]
      public void Verification_Can_Fail()
      {
         var knownPrivateKeyPEM = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VwBCIEIMFjaUZrHJYPJH4O2bPTsnFwqXsGTVRooB2jw78TnGjH
gSEARRpYb3MlC/w8giB4NsNrKvPsnfuVsXBlHFywuEfJQQo=
-----END PRIVATE KEY-----";

         var knownPrivateKey = KeyConversion.ConvertEd25519PrivateKeyFromPEM(knownPrivateKeyPEM);
         var knownPublicKey = knownPrivateKey.GeneratePublicKey();

         var badPlaintext = new byte[]
         {
            0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99,
            0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99
         };

         var knownSignature = new byte[]
         {
            0xee, 0x14, 0xe8, 0x70, 0x22, 0x5f, 0xce, 0x8f,
            0xa4, 0x02, 0x39, 0x64, 0xcb, 0x77, 0x70, 0xdf,
            0x5b, 0x50, 0x22, 0x72, 0x92, 0x7e, 0xc6, 0x24,
            0xd6, 0x0d, 0xc9, 0x7d, 0xd7, 0x51, 0xbc, 0xe7,
            0xbc, 0x3b, 0x20, 0x79, 0x68, 0x6b, 0xf6, 0x2c,
            0x5f, 0x0c, 0x8e, 0xe5, 0xe3, 0xe4, 0x6b, 0xc9,
            0xa7, 0xd3, 0x10, 0xe0, 0x85, 0xea, 0x37, 0x9e,
            0x35, 0x0d, 0x80, 0xef, 0xf2, 0xef, 0xf9, 0x00
         };

         var verifier = new ECDSA();
         verifier.InitializeVerifier(knownPublicKey);
         verifier.VerifierDigestChunk(badPlaintext);
         var verificationResult = verifier.VerifySignature(knownSignature);

         Assert.False(verificationResult);
      }
   }
}
