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

         var knownPem = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VuBCIEIMgIsnIoed7vL0TR4K7XLj/XLKMi3e+Sb5ZQRjxA5Lpg
gSEAj5qsk+931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PRIVATE KEY-----
".ReplaceLineEndings();

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

         var knownPem = @"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VuAyEAj5qskz931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PUBLIC KEY-----
".ReplaceLineEndings();

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

         var knownPem = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VuBCIEIMgIsnIoed7vL0TR4K7XLj/XLKMi3e+Sb5ZQRjxA5Lpg
gSEAj5qsk+931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PRIVATE KEY-----
".ReplaceLineEndings();

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

         var knownPem = @"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VuAyEAj5qskz931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PUBLIC KEY-----
".ReplaceLineEndings();

         var publicKey = KeyConversion.ConvertX25519PublicKeyFromPEM(knownPem);

         Assert.AreEqual(knownPublicKey, publicKey.GetEncoded());
      }

      [Test]
      public void Convert_Ed25519_Private_Key_To_PEM_Is_Consistent()
      {
         var knownPrivateKey = new byte[]
         {
            0xbc, 0x92, 0x78, 0x5e, 0x88, 0xb7, 0x53, 0xcb,
            0x96, 0x2f, 0x84, 0xb3, 0xa2, 0xc8, 0x59, 0x1c,
            0x56, 0xcc, 0x4e, 0xad, 0xe2, 0x91, 0xf8, 0x83,
            0xcc, 0xb0, 0xa7, 0x08, 0x66, 0x2d, 0x51, 0xde
         };

         var knownPem = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VwBCIEILySeF6It1PLli+Es6LIWRxWzE6t4pH4g8ywpwhmLVHe
gSEAyTUnqI1OY2JSoXWosTJYszV6J6AXSCAVu+24eWgh89M=
-----END PRIVATE KEY-----
".ReplaceLineEndings();

         var privateKey = new Ed25519PrivateKeyParameters(knownPrivateKey);
         var pem = privateKey.ConvertToPEM();

         Assert.AreEqual(knownPem, pem);
      }

      [Test]
      public void Convert_Ed25519_Public_Key_To_PEM_Is_Consistent()
      {
         var knownPublicKey = new byte[]
         {
            0xc9, 0x35, 0x27, 0xa8, 0x8d, 0x4e, 0x63, 0x62,
            0x52, 0xa1, 0x75, 0xa8, 0xb1, 0x32, 0x58, 0xb3,
            0x35, 0x7a, 0x27, 0xa0, 0x17, 0x48, 0x20, 0x15,
            0xbb, 0xed, 0xb8, 0x79, 0x68, 0x21, 0xf3, 0xd3
         };

         var knownPem = @"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEAyTUnqI1OY2JSoXWosTJYszV6J6AXSCAVu+24eWgh89M=
-----END PUBLIC KEY-----
".ReplaceLineEndings();

         var publicKey = new Ed25519PublicKeyParameters(knownPublicKey);
         var pem = publicKey.ConvertToPEM();

         Assert.AreEqual(knownPem, pem);
      }

      [Test]
      public void Convert_Ed25519_Private_Key_From_PEM_Is_Consistent()
      {
         var knownPrivateKey = new byte[]
         {
            0xbc, 0x92, 0x78, 0x5e, 0x88, 0xb7, 0x53, 0xcb,
            0x96, 0x2f, 0x84, 0xb3, 0xa2, 0xc8, 0x59, 0x1c,
            0x56, 0xcc, 0x4e, 0xad, 0xe2, 0x91, 0xf8, 0x83,
            0xcc, 0xb0, 0xa7, 0x08, 0x66, 0x2d, 0x51, 0xde
         };

         var knownPem = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VwBCIEILySeF6It1PLli+Es6LIWRxWzE6t4pH4g8ywpwhmLVHe
gSEAyTUnqI1OY2JSoXWosTJYszV6J6AXSCAVu+24eWgh89M=
-----END PRIVATE KEY-----
".ReplaceLineEndings();

         var privateKey = KeyConversion.ConvertEd25519PrivateKeyFromPEM(knownPem);

         Assert.AreEqual(knownPrivateKey, privateKey.GetEncoded());
      }

      [Test]
      public void Convert_Ed25519_Public_Key_From_PEM_Is_Consistent()
      {
         var knownPublicKey = new byte[]
         {
            0xc9, 0x35, 0x27, 0xa8, 0x8d, 0x4e, 0x63, 0x62,
            0x52, 0xa1, 0x75, 0xa8, 0xb1, 0x32, 0x58, 0xb3,
            0x35, 0x7a, 0x27, 0xa0, 0x17, 0x48, 0x20, 0x15,
            0xbb, 0xed, 0xb8, 0x79, 0x68, 0x21, 0xf3, 0xd3
         };

         var knownPem = @"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEAyTUnqI1OY2JSoXWosTJYszV6J6AXSCAVu+24eWgh89M=
-----END PUBLIC KEY-----
".ReplaceLineEndings();

         var publicKey = KeyConversion.ConvertEd25519PublicKeyFromPEM(knownPem);

         Assert.AreEqual(knownPublicKey, publicKey.GetEncoded());
      }
   }
}
