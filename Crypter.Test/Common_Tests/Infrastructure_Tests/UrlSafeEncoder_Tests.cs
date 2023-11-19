/*
 * Copyright (C) 2023 Crypter File Transfer
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

using System;
using Crypter.Common.Infrastructure;
using NUnit.Framework;

namespace Crypter.Test.Common_Tests.Infrastructure_Tests;

[TestFixture]
public class UrlSafeEncoder_Tests
{
   [SetUp]
   public void Setup()
   {
   }

   [Test]
   public void Code_Encoding_Is_Consistent()
   {
      Guid code = new Guid("adec7c67-dfb6-4b9f-9a1f-04c0b0f4643c");
      string knownEncoding = "Z3zsrbbfn0uaHwTAsPRkPA";
      string newEncoding = UrlSafeEncoder.EncodeGuidUrlSafe(code);
      Assert.AreEqual(knownEncoding, newEncoding);
   }

   [Test]
   public void Code_Decoding_Is_Consistent()
   {
      string encoding = "Z3zsrbbfn0uaHwTAsPRkPA";
      Guid knownCode = new Guid("adec7c67-dfb6-4b9f-9a1f-04c0b0f4643c");
      Guid newCode = UrlSafeEncoder.DecodeGuidFromUrlSafe(encoding);
      Assert.AreEqual(knownCode, newCode);
   }

   [Test]
   public void Signature_Encoding_Is_Consistent()
   {
      byte[] bytes = new byte[] {
         0x8b, 0x6f, 0xa0, 0x13, 0x13, 0xce, 0x51, 0xaf,
         0xc0, 0x9e, 0x61, 0x0f, 0x81, 0x92, 0x50, 0xda,
         0x50, 0x17, 0x78, 0xad, 0x36, 0x3c, 0xba, 0x4f,
         0x9e, 0x31, 0x2a, 0x6e, 0xc8, 0x23, 0xd4, 0x2a
      };
      string knownEncoding = "i2-gExPOUa_AnmEPgZJQ2lAXeK02PLpPnjEqbsgj1Co";
      string newEncoding = UrlSafeEncoder.EncodeBytesUrlSafe(bytes);
      Assert.AreEqual(knownEncoding, newEncoding);
   }

   [Test]
   public void Signature_Decoding_Is_Consistent()
   {
      string encoding = "i2-gExPOUa_AnmEPgZJQ2lAXeK02PLpPnjEqbsgj1Co";
      byte[] knownBytes = new byte[] {
         0x8b, 0x6f, 0xa0, 0x13, 0x13, 0xce, 0x51, 0xaf,
         0xc0, 0x9e, 0x61, 0x0f, 0x81, 0x92, 0x50, 0xda,
         0x50, 0x17, 0x78, 0xad, 0x36, 0x3c, 0xba, 0x4f,
         0x9e, 0x31, 0x2a, 0x6e, 0xc8, 0x23, 0xd4, 0x2a
      };
      byte[] newSignature = UrlSafeEncoder.DecodeBytesFromUrlSafe(encoding);
      Assert.AreEqual(knownBytes, newSignature);
   }
}