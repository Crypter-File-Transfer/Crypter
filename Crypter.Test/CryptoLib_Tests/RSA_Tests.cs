using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Enums;
using NUnit.Framework;

namespace Crypter.Test.CryptoLib_Tests
{
   public class RSA_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void KeyGen_Works()
      {
         Assert.DoesNotThrow(() => RSA.GenerateKeys(RsaKeySize._1024));
      }

      [Test]
      public void KeyGen_Produces_Unique_Keys()
      {
         string privateKey1 = RSA.GenerateKeys(RsaKeySize._512).Private.ConvertToPEM();
         string privateKey2 = RSA.GenerateKeys(RsaKeySize._512).Private.ConvertToPEM();

         Assert.AreNotEqual(privateKey1, privateKey2);
      }

      [Test]
      public void Encryption_Is_Predictable()
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

         var knownPlaintext = new byte[]
         {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10
         };

         var knownCiphertext = new byte[]
         {
            0x02, 0x81, 0xa1, 0x64, 0x55, 0xd9, 0xd4, 0xdc,
            0xfa, 0xdc, 0xec, 0xb6, 0x63, 0xa7, 0x6f, 0xa7,
            0x8c, 0xa4, 0xc1, 0x94, 0x6c, 0x23, 0x1f, 0x30,
            0x26, 0xd7, 0xb1, 0x6a, 0x96, 0x4b, 0xa0, 0x45,
            0xc1, 0x17, 0x66, 0x54, 0xa5, 0x81, 0x5e, 0x26,
            0x84, 0x4c, 0xc5, 0xcf, 0xd7, 0xaf, 0xe5, 0xc0,
            0xfe, 0x75, 0x69, 0x32, 0x00, 0x94, 0xee, 0x9c,
            0xaa, 0x1e, 0x29, 0x59, 0x14, 0x6a, 0x5f, 0x4b
         };

         var loadedKeys = KeyConversion.ConvertRSAPrivateKeyFromPEM(knownPrivateKey);
         var newCiphertext = RSA.Encrypt(knownPlaintext, loadedKeys.Public);
         Assert.AreEqual(knownCiphertext, newCiphertext);
      }

      [Test]
      public void Decryption_Is_Predictable()
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

         var knownPlaintext = new byte[]
         {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10
         };

         var knownCiphertext = new byte[]
         {
            0x02, 0x81, 0xa1, 0x64, 0x55, 0xd9, 0xd4, 0xdc,
            0xfa, 0xdc, 0xec, 0xb6, 0x63, 0xa7, 0x6f, 0xa7,
            0x8c, 0xa4, 0xc1, 0x94, 0x6c, 0x23, 0x1f, 0x30,
            0x26, 0xd7, 0xb1, 0x6a, 0x96, 0x4b, 0xa0, 0x45,
            0xc1, 0x17, 0x66, 0x54, 0xa5, 0x81, 0x5e, 0x26,
            0x84, 0x4c, 0xc5, 0xcf, 0xd7, 0xaf, 0xe5, 0xc0,
            0xfe, 0x75, 0x69, 0x32, 0x00, 0x94, 0xee, 0x9c,
            0xaa, 0x1e, 0x29, 0x59, 0x14, 0x6a, 0x5f, 0x4b
         };

         var loadedKeys = KeyConversion.ConvertRSAPrivateKeyFromPEM(knownPrivateKey);
         var newPlaintext = RSA.Decrypt(knownCiphertext, loadedKeys.Private);
         Assert.AreEqual(knownPlaintext, newPlaintext);
      }
   }
}
