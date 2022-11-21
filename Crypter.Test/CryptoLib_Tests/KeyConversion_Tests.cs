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

using Crypter.Common.Primitives;
using Crypter.CryptoLib;
using NUnit.Framework;
using Org.BouncyCastle.Crypto.Parameters;

namespace Crypter.Test.CryptoLib_Tests
{
   [TestFixture]
   public class KeyConversion_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void Convert_X25519_Private_Key_To_PEM_Is_Consistent()
      {
         var knownPrivateKey = new byte[]
         {
            0xc8, 0x08, 0xb2, 0x72, 0x28, 0x79, 0xde, 0xef,
            0x2f, 0x44, 0xd1, 0xe0, 0xae, 0xd7, 0x2e, 0x3f,
            0xd7, 0x2c, 0xa3, 0x22, 0xdd, 0xef, 0x92, 0x6f,
            0x96, 0x50, 0x46, 0x3c, 0x40, 0xe4, 0xba, 0x60
         };

         var knownPem = PEMString.From(@"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VuBCIEIMgIsnIoed7vL0TR4K7XLj/XLKMi3e+Sb5ZQRjxA5Lpg
gSEAj5qsk+931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PRIVATE KEY-----
".ReplaceLineEndings());

         var privateKey = new X25519PrivateKeyParameters(knownPrivateKey);
         var pem = privateKey.ConvertToPEM();

         Assert.AreEqual(knownPem, pem);
      }

      [Test]
      public void Convert_X25519_Public_Key_To_PEM_Is_Consistent()
      {
         var knownPublicKey = new byte[]
         {
            0x8f, 0x9a, 0xac, 0x93, 0x3f, 0x77, 0xd7, 0x1a,
            0x70, 0x1d, 0x7a, 0xcd, 0xe3, 0x4a, 0x67, 0xc5,
            0x74, 0x84, 0xcf, 0x4f, 0x07, 0xc6, 0xe8, 0x70,
            0xdb, 0x00, 0x01, 0x97, 0xe1, 0x86, 0xf7, 0x20
         };

         var knownPem = PEMString.From(@"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VuAyEAj5qskz931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PUBLIC KEY-----
".ReplaceLineEndings());

         var publicKey = new X25519PublicKeyParameters(knownPublicKey);
         var pem = publicKey.ConvertToPEM();

         Assert.AreEqual(knownPem, pem);
      }

      [Test]
      public void Convert_X25519_Private_Key_From_PEM_Is_Consistent()
      {
         var knownPrivateKey = new byte[]
         {
            0xc8, 0x08, 0xb2, 0x72, 0x28, 0x79, 0xde, 0xef,
            0x2f, 0x44, 0xd1, 0xe0, 0xae, 0xd7, 0x2e, 0x3f,
            0xd7, 0x2c, 0xa3, 0x22, 0xdd, 0xef, 0x92, 0x6f,
            0x96, 0x50, 0x46, 0x3c, 0x40, 0xe4, 0xba, 0x60
         };

         var knownPem = PEMString.From(@"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VuBCIEIMgIsnIoed7vL0TR4K7XLj/XLKMi3e+Sb5ZQRjxA5Lpg
gSEAj5qsk+931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PRIVATE KEY-----
".ReplaceLineEndings());

         var privateKey = KeyConversion.ConvertX25519PrivateKeyFromPEM(knownPem);

         Assert.AreEqual(knownPrivateKey, privateKey.GetEncoded());
      }

      [Test]
      public void Convert_X25519_Public_Key_From_PEM_Is_Consistent()
      {
         var knownPublicKey = new byte[]
         {
            0x8f, 0x9a, 0xac, 0x93, 0x3f, 0x77, 0xd7, 0x1a,
            0x70, 0x1d, 0x7a, 0xcd, 0xe3, 0x4a, 0x67, 0xc5,
            0x74, 0x84, 0xcf, 0x4f, 0x07, 0xc6, 0xe8, 0x70,
            0xdb, 0x00, 0x01, 0x97, 0xe1, 0x86, 0xf7, 0x20
         };

         var knownPem = PEMString.From(@"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VuAyEAj5qskz931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PUBLIC KEY-----
".ReplaceLineEndings());

         var publicKey = KeyConversion.ConvertX25519PublicKeyFromPEM(knownPem);

         Assert.AreEqual(knownPublicKey, publicKey.GetEncoded());
      }
   }
}
