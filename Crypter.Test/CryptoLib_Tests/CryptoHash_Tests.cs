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

using Crypter.CryptoLib.SodiumLib;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace Crypter.Test.CryptoLib_Tests
{
   [TestFixture]
   internal class CryptoHash_Tests
   {
      [Test]
      public void Sha256_Digests_Are_Unique()
      {
         byte[] firstInput = new byte[]
         {
            0x01, 0x02, 0x03, 0x04
         };

         byte[] secondInput = new byte[]
         {
            0x02, 0x03, 0x04, 0x05
         };

         byte[] firstOutput = CryptoHash.Sha256(firstInput);
         byte[] secondOutput = CryptoHash.Sha256(secondInput);

         Assert.AreNotEqual(firstOutput, secondOutput);
      }

      [Test]
      public void Sha512_Digests_Are_Unique()
      {
         byte[] firstInput = new byte[]
{
            0x01, 0x02, 0x03, 0x04
};

         byte[] secondInput = new byte[]
         {
            0x02, 0x03, 0x04, 0x05
         };

         byte[] firstOutput = CryptoHash.Sha512(firstInput);
         byte[] secondOutput = CryptoHash.Sha512(secondInput);

         Assert.AreNotEqual(firstOutput, secondOutput);
      }

      [Test]
      public void Sha256_Digest_Is_Predictable()
      {
         byte[] knownInput = new byte[] {
            0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20
         };

         byte[] knownHash = new byte[] {
            0x8b, 0x6f, 0xa0, 0x13, 0x13, 0xce, 0x51, 0xaf,
            0xc0, 0x9e, 0x61, 0x0f, 0x81, 0x92, 0x50, 0xda,
            0x50, 0x17, 0x78, 0xad, 0x36, 0x3c, 0xba, 0x4f,
            0x9e, 0x31, 0x2a, 0x6e, 0xc8, 0x23, 0xd4, 0x2a
         };

         byte[] digest = CryptoHash.Sha256(knownInput);
         Assert.AreEqual(knownHash, digest);
      }

      [Test]
      public void Sha512_Digest_Is_Predictable()
      {
         byte[] knownInput = new byte[] {
            0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20
         };

         byte[] knownHash = new byte[] {
            0xe3, 0x9e, 0x5f, 0xa9, 0x17, 0x94, 0xc9, 0xd7,
            0x52, 0x73, 0x13, 0x43, 0x3d, 0xcc, 0xf7, 0xc4,
            0xec, 0xc0, 0xea, 0xc5, 0x62, 0x67, 0x4e, 0xb6,
            0xee, 0x9b, 0x6e, 0x30, 0x43, 0x4c, 0x92, 0xd3,
            0x35, 0x0b, 0xf6, 0xd9, 0xff, 0x7a, 0x29, 0x9c,
            0x14, 0x34, 0x23, 0xed, 0x74, 0x93, 0x19, 0x75,
            0xdd, 0x16, 0xdf, 0x76, 0x1d, 0x8f, 0x0c, 0xb7,
            0x5b, 0x58, 0x7c, 0x48, 0x4b, 0x54, 0x63, 0xab
         };

         byte[] digest = CryptoHash.Sha512(knownInput);
         Assert.AreEqual(knownHash, digest);
      }

      [Test]
      public void Sha256_String_Digest_Matches_Byte_Digest()
      {
         string knownInputString = "test";
         byte[] knownInputBytes = Encoding.UTF8.GetBytes(knownInputString);

         byte[] stringDigest = CryptoHash.Sha256(knownInputString);
         byte[] byteDigest = CryptoHash.Sha256(knownInputBytes);

         Assert.AreEqual(byteDigest, stringDigest);
      }

      [Test]
      public void Sha512_String_Digest_Matches_Byte_Digest()
      {
         string knownInputString = "test";
         byte[] knownInputBytes = Encoding.UTF8.GetBytes(knownInputString);

         byte[] stringDigest = CryptoHash.Sha512(knownInputString);
         byte[] byteDigest = CryptoHash.Sha512(knownInputBytes);

         Assert.AreEqual(byteDigest, stringDigest);
      }

      [Test]
      public void Sha256_List_Produces_Same_Digest()
      {
         List<byte[]> inputList = new List<byte[]>
         {
            new byte[] { 0x00, 0x01, 0x02, 0x03 },
            new byte[] { 0x04, 0x05, 0x06, 0x07 }
         };

         byte[] inputFlat = new byte[]
         {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07
         };

         byte[] listDigest = CryptoHash.Sha256(inputList);
         byte[] flatDigest = CryptoHash.Sha256(inputFlat);

         Assert.AreEqual(flatDigest, listDigest);
      }

      [Test]
      public void Sha512_List_Produces_Same_Digest()
      {
         List<byte[]> inputList = new List<byte[]>
         {
            new byte[] { 0x00, 0x01, 0x02, 0x03 },
            new byte[] { 0x04, 0x05, 0x06, 0x07 }
         };

         byte[] inputFlat = new byte[]
         {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07
         };

         byte[] listDigest = CryptoHash.Sha512(inputList);
         byte[] flatDigest = CryptoHash.Sha512(inputFlat);

         Assert.AreEqual(flatDigest, listDigest);
      }
   }
}
