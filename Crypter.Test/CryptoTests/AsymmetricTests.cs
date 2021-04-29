using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Enums;
using NUnit.Framework;

namespace Crypter.Test.CryptoTests
{
   public class AsymmetricTests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Asymmetric_KeyGen_Works()
      {
         // Doing additional verification on the keys themselves is pretty difficult
         // Will need to eventually open up the keys and verify their contents
         // Great write-up: https://hackernoon.com/public-key-cryptography-rsa-keys-izda3ylv

         // If you inspect a Private or Public key while debugging, you can actually see the values that make up the key
         // BouncyCastle (or how we are currently using it) does not expose those values to us in the code

         Assert.DoesNotThrow(() => AsymmetricMethods.GenerateKeys(RsaKeySize._1024));
      }

      [Test]
      public void Asymmetric_KeyGen_Produces_Unique_Keys()
      {
         string privateKey1 = AsymmetricMethods.GenerateKeys(RsaKeySize._512).Private.ConvertToPEM();
         string privateKey2 = AsymmetricMethods.GenerateKeys(RsaKeySize._512).Private.ConvertToPEM();

         Assert.AreNotEqual(privateKey1, privateKey2);
      }

      [Test]
      public void Asymmetric_Encryption_Is_Predictable()
      {
         var knownPrivateKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIBOgIBAAJBAJCiNjSRqZgk+cAzf67t/wSFnFTTRQ5zlN4kbdOOceNEaEcEFgp+
SaEoWKOOsdQNUya+zQ3TeAytIjOT9lSErTkCAwEAAQJAAu3B9r0MsofB0Jj1CNwJ
ZLRMQYcjWBhSPKWqCAATrCQuky7IbnT/W1R5kInHNhIiRV+t7kmTeZMB76aCVlF8
vQIhANCjmyt3t+fpKcrUg8gLt8KsASOvIKFoE5qgL+KD/d8VAiEAsXciMeKy/6Tl
rG4Gq2AAv8mqEkB93ic/XGYmPOd//pUCIQCYR/HHxjfK4xoH2xjceAEF67lhLD+q
z2YPo/+PWzt/CQIgc4JolnHJMo6BE7+1xZxCQJMhiKnDg3KmUh0G7IN+ExUCIF5l
2zoR2BRJjNEpn4SSIuv1D87yFG8wlcgxeTCl1/yk
-----END RSA PRIVATE KEY-----";

         /*
         var knownPublicKey = @"-----BEGIN PUBLIC KEY-----
MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAJCiNjSRqZgk+cAzf67t/wSFnFTTRQ5z
lN4kbdOOceNEaEcEFgp+SaEoWKOOsdQNUya+zQ3TeAytIjOT9lSErTkCAwEAAQ==
-----END PUBLIC KEY-----";
         */

         var knownPlaintext = new byte[]
         {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f
         };

         var knownCiphertext = new byte[]
         {
            0x43, 0x76, 0x59, 0xc0, 0x62, 0x87, 0xd3, 0xc1,
            0x96, 0x5e, 0xf0, 0x6d, 0xe3, 0x44, 0xa9, 0x20,
            0xe9, 0x80, 0x2d, 0xd5, 0x6f, 0xfe, 0x56, 0x5f,
            0xe5, 0xc9, 0x7c, 0xc2, 0x45, 0xe7, 0xee, 0xfb,
            0xab, 0xc8, 0xc4, 0xcf, 0xa9, 0x6c, 0x36, 0xd9,
            0x26, 0x10, 0x8f, 0x55, 0xe9, 0xf5, 0xa2, 0x84,
            0x35, 0x32, 0x08, 0xd9, 0x92, 0x8d, 0xef, 0x70,
            0x75, 0x56, 0xf4, 0x29, 0xee, 0x5b, 0xa0, 0xc4
         };

         var loadedKeys = CryptoLib.Common.ConvertRsaPrivateKeyFromPEM(knownPrivateKey);
         var newCiphertext = AsymmetricMethods.Encrypt(knownPlaintext, loadedKeys.Public);
         Assert.AreEqual(knownCiphertext, newCiphertext);
      }
   }
}
