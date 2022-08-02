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
using Crypter.CryptoLib.Models;
using NUnit.Framework;

namespace Crypter.Test
{
   [TestFixture]
   internal class PublicKeyAuth_Tests
   {
      [Test]
      public void Constant_Values_Have_Not_Changed()
      {
         Assert.AreEqual(64, PublicKeyAuth.PrivateKeyBytes);
         Assert.AreEqual(32, PublicKeyAuth.PublicKeyBytes);
         Assert.AreEqual(64, PublicKeyAuth.SignatureBytes);
         Assert.AreEqual(32, PublicKeyAuth.SeedBytes);
      }

      [Test]
      public void Generated_Key_Pairs_Are_Unique()
      {
         AsymmetricKeyPair firstKeyPair = PublicKeyAuth.GenerateKeyPair();
         AsymmetricKeyPair secondKeyPair = PublicKeyAuth.GenerateKeyPair();

         Assert.AreNotEqual(firstKeyPair.PrivateKey, firstKeyPair.PublicKey);
         Assert.AreNotEqual(secondKeyPair.PrivateKey, secondKeyPair.PublicKey);

         Assert.AreNotEqual(firstKeyPair.PrivateKey, secondKeyPair.PrivateKey);
         Assert.AreNotEqual(firstKeyPair.PublicKey, secondKeyPair.PublicKey);
      }

      [Test]
      public void Generated_Keys_Have_Constant_Lengths()
      {
         AsymmetricKeyPair keyPair = PublicKeyAuth.GenerateKeyPair();

         Assert.AreEqual(PublicKeyAuth.PrivateKeyBytes, keyPair.PrivateKey.Length);
         Assert.AreEqual(PublicKeyAuth.PublicKeyBytes, keyPair.PublicKey.Length);
      }

      [Test]
      public void Generated_Seed_Are_Unique()
      {
         byte[] firstSeed = PublicKeyAuth.GenerateSeed();
         byte[] secondSeed = PublicKeyAuth.GenerateSeed();

         Assert.AreNotEqual(firstSeed, secondSeed);
         Assert.AreEqual(PublicKeyAuth.SeedBytes, firstSeed.Length);
         Assert.AreEqual(PublicKeyAuth.SeedBytes, secondSeed.Length);
      }

      [Test]
      public void Seeded_Key_Pairs_Are_Unique()
      {
         byte[] firstSeed = PublicKeyAuth.GenerateSeed();
         byte[] secondSeed = PublicKeyAuth.GenerateSeed();

         AsymmetricKeyPair firstKeyPair = PublicKeyAuth.GenerateSeededKeyPair(firstSeed);
         AsymmetricKeyPair secondKeyPair = PublicKeyAuth.GenerateSeededKeyPair(secondSeed);

         Assert.AreNotEqual(firstKeyPair.PrivateKey, secondKeyPair.PrivateKey);
         Assert.AreNotEqual(firstKeyPair.PublicKey, secondKeyPair.PublicKey);
      }

      [Test]
      public void Known_Seed_Produces_Same_Key_Pair()
      {
         byte[] seed = new byte[]
         {
            0x7f, 0x74, 0xd8, 0x06, 0x10, 0xe0, 0xf0, 0xd8,
            0xa8, 0x2c, 0xc2, 0x9c, 0x02, 0x90, 0x03, 0x91,
            0x66, 0x02, 0xbb, 0x2e, 0xd1, 0xdc, 0x83, 0x0b,
            0x68, 0x95, 0x34, 0xe4, 0x0f, 0xd2, 0xe8, 0xe4
         };

         byte[] knownPrivateKey = new byte[]
         {
            0x7f, 0x74, 0xd8, 0x06, 0x10, 0xe0, 0xf0, 0xd8,
            0xa8, 0x2c, 0xc2, 0x9c, 0x02, 0x90, 0x03, 0x91,
            0x66, 0x02, 0xbb, 0x2e, 0xd1, 0xdc, 0x83, 0x0b,
            0x68, 0x95, 0x34, 0xe4, 0x0f, 0xd2, 0xe8, 0xe4,
            0x95, 0x7c, 0xf8, 0x46, 0x96, 0x62, 0x42, 0x78,
            0xbf, 0x0f, 0x2f, 0x61, 0xd7, 0x9a, 0xeb, 0x33,
            0x3d, 0xf1, 0xea, 0x79, 0x13, 0x7b, 0xcf, 0x96,
            0xcb, 0x11, 0x98, 0xf8, 0xbf, 0xce, 0xce, 0xf1
         };

         byte[] knownPublicKey = new byte[]
         {
            0x95, 0x7c, 0xf8, 0x46, 0x96, 0x62, 0x42, 0x78,
            0xbf, 0x0f, 0x2f, 0x61, 0xd7, 0x9a, 0xeb, 0x33,
            0x3d, 0xf1, 0xea, 0x79, 0x13, 0x7b, 0xcf, 0x96,
            0xcb, 0x11, 0x98, 0xf8, 0xbf, 0xce, 0xce, 0xf1
         };

         AsymmetricKeyPair keyPair = PublicKeyAuth.GenerateSeededKeyPair(seed);

         Assert.AreEqual(knownPrivateKey, keyPair.PrivateKey);
         Assert.AreEqual(knownPublicKey, keyPair.PublicKey);
      }

      [Test]
      public void Known_Private_Key_Produces_Same_Public_Key()
      {
         byte[] knownPrivateKey = new byte[]
         {
            0x28, 0xe1, 0x1b, 0x03, 0x56, 0x67, 0x30, 0xdf,
            0x27, 0xf6, 0xbe, 0xa3, 0x44, 0x88, 0xb8, 0x14,
            0xe8, 0xa4, 0xec, 0x59, 0xe8, 0x1c, 0x49, 0x6c,
            0xd0, 0xfb, 0x30, 0xb7, 0x2d, 0xdb, 0x17, 0x2b,
            0x2a, 0xd0, 0x5c, 0xed, 0xd7, 0x36, 0xf0, 0x07,
            0xde, 0x0f, 0xd4, 0xb4, 0x40, 0x2b, 0xb9, 0x31,
            0x3f, 0xe6, 0x73, 0x9b, 0xb2, 0xff, 0xe1, 0x98,
            0x4c, 0x8b, 0xa4, 0x2c, 0x2a, 0x19, 0x12, 0xf7
         };

         byte[] knownPublicKey = new byte[]
         {
            0x2a, 0xd0, 0x5c, 0xed, 0xd7, 0x36, 0xf0, 0x07,
            0xde, 0x0f, 0xd4, 0xb4, 0x40, 0x2b, 0xb9, 0x31,
            0x3f, 0xe6, 0x73, 0x9b, 0xb2, 0xff, 0xe1, 0x98,
            0x4c, 0x8b, 0xa4, 0x2c, 0x2a, 0x19, 0x12, 0xf7
         };

         byte[] generatedPublicKey = PublicKeyAuth.GetPublicKey(knownPrivateKey);
         Assert.AreEqual(knownPublicKey, generatedPublicKey);
      }

      [Test]
      public void Signatures_Are_Unique()
      {
         AsymmetricKeyPair firstKeyPair = PublicKeyAuth.GenerateKeyPair();
         AsymmetricKeyPair secondKeyPair = PublicKeyAuth.GenerateKeyPair();

         byte[] message = { 0x00, 0x01, 0x02, 0x03 };

         byte[] firstSignature = PublicKeyAuth.Sign(message, firstKeyPair.PrivateKey);
         byte[] secondSignature = PublicKeyAuth.Sign(message, secondKeyPair.PrivateKey);

         Assert.AreNotEqual(firstKeyPair.PrivateKey, secondKeyPair.PrivateKey);
         Assert.AreNotEqual(firstSignature, secondSignature);
         Assert.AreEqual(PublicKeyAuth.SignatureBytes, firstSignature.Length);
         Assert.AreEqual(PublicKeyAuth.SignatureBytes, secondSignature.Length);
      }

      [Test]
      public void Signatures_Are_Deterministic()
      {
         byte[] privateKey = new byte[]
         {
            0x7f, 0x74, 0xd8, 0x06, 0x10, 0xe0, 0xf0, 0xd8,
            0xa8, 0x2c, 0xc2, 0x9c, 0x02, 0x90, 0x03, 0x91,
            0x66, 0x02, 0xbb, 0x2e, 0xd1, 0xdc, 0x83, 0x0b,
            0x68, 0x95, 0x34, 0xe4, 0x0f, 0xd2, 0xe8, 0xe4,
            0x95, 0x7c, 0xf8, 0x46, 0x96, 0x62, 0x42, 0x78,
            0xbf, 0x0f, 0x2f, 0x61, 0xd7, 0x9a, 0xeb, 0x33,
            0x3d, 0xf1, 0xea, 0x79, 0x13, 0x7b, 0xcf, 0x96,
            0xcb, 0x11, 0x98, 0xf8, 0xbf, 0xce, 0xce, 0xf1
         };

         byte[] message = { 0x00, 0x01, 0x02, 0x03 };

         byte[] knownSigature = new byte[]
         {
            0x0b, 0x2a, 0x76, 0xb1, 0xf7, 0x59, 0xdb, 0x33,
            0xf4, 0x72, 0x03, 0xe1, 0x76, 0x7e, 0xca, 0xc8,
            0x16, 0x75, 0x57, 0x72, 0xf4, 0x01, 0xc9, 0x1e,
            0x9d, 0x6e, 0xf5, 0xdb, 0x43, 0x48, 0xc1, 0x61,
            0xd0, 0xda, 0x77, 0xee, 0x7d, 0x7f, 0x9b, 0x33,
            0xf9, 0xbe, 0xef, 0x19, 0xdb, 0x7b, 0x09, 0x96,
            0xaa, 0xac, 0x4d, 0xa9, 0xe8, 0x0a, 0x26, 0x8f,
            0xd2, 0x22, 0x06, 0xca, 0xe2, 0x7c, 0xfc, 0x04
         };

         byte[] signature = PublicKeyAuth.Sign(message, privateKey);
         Assert.AreEqual(knownSigature, signature);
      }

      [Test]
      public void Verification_Succeeds_With_Valid_Signature()
      {
         AsymmetricKeyPair keyPair = PublicKeyAuth.GenerateKeyPair();
         byte[] message = { 0x00, 0x01, 0x02, 0x03 };
         byte[] signature = PublicKeyAuth.Sign(message, keyPair.PrivateKey);

         bool verificationResult = PublicKeyAuth.Verify(message, signature, keyPair.PublicKey);
         Assert.True(verificationResult);
      }

      [Test]
      public void Verification_Fails_With_Invalid_Signature()
      {
         AsymmetricKeyPair keyPair = PublicKeyAuth.GenerateKeyPair();
         byte[] message = { 0x00, 0x01, 0x02, 0x03 };
         byte[] signature = PublicKeyAuth.Sign(message, keyPair.PrivateKey);

         byte[] dirtyMessage = { 0x01, 0x01, 0x02, 0x03 };

         byte[] dirtySignature = new byte[signature.Length];
         dirtySignature[0] = (byte)(dirtySignature[0] == 0x01
            ? 0x02
            : 0x01);

         bool dirtySignatureVerificationResult = PublicKeyAuth.Verify(message, dirtySignature, keyPair.PublicKey);
         Assert.False(dirtySignatureVerificationResult);

         bool dirtyMessageVerificationResult = PublicKeyAuth.Verify(dirtyMessage, signature, keyPair.PublicKey);
         Assert.False(dirtyMessageVerificationResult);
      }
   }
}
