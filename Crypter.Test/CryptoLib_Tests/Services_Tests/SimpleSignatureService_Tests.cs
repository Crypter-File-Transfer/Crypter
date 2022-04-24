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

using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.CryptoLib.Services;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.Test.CryptoLib_Tests.Services_Tests
{
   [TestFixture]
   public class SimpleSignatureService_Tests
   {
      private PEMString _knownPrivatePEM;
      private PEMString _knownPublicPEM;
      private byte[] _knownData;
      private byte[] _knownSignature;
      private SimpleSignatureService _sut;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _knownPrivatePEM = PEMString.From(@"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VwBCIEIMFjaUZrHJYPJH4O2bPTsnFwqXsGTVRooB2jw78TnGjH
gSEARRpYb3MlC/w8giB4NsNrKvPsnfuVsXBlHFywuEfJQQo=
-----END PRIVATE KEY-----");

         _knownPublicPEM = PEMString.From(@"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VwAyEARRpYb3MlC/w8giB4NsNrKvPsnfuVsXBlHFywuEfJQQo=
-----END PUBLIC KEY-----");

         _knownData = new byte[]
         {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10
         };

         _knownSignature = new byte[]
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

         _sut = new SimpleSignatureService();
      }

      [Test]
      public void Signing_Is_Predicatable()
      {
         var newSignature = _sut.Sign(_knownPrivatePEM, _knownData);
         Assert.AreEqual(_knownSignature, newSignature);
      }

      [Test]
      public void Verification_Can_Succeed()
      {
         bool verificationSuccess = _sut.Verify(_knownPublicPEM, _knownData, _knownSignature);
         Assert.IsTrue(verificationSuccess);
      }

      [Test]
      public void Verification_Can_Fail_From_Bad_Data()
      {
         byte[] badData = new byte[_knownData.Length];
         _knownData.CopyTo(badData, 0);
         badData[0]--;

         bool verificationSuccess = _sut.Verify(_knownPublicPEM, badData, _knownSignature);
         Assert.IsFalse(verificationSuccess);
      }

      [Test]
      public void Verification_Can_Fail_From_Bad_Signature()
      {
         byte[] badSignature = new byte[_knownSignature.Length];
         _knownSignature.CopyTo(badSignature, 0);
         badSignature[0]--;

         bool verificationSuccess = _sut.Verify(_knownPublicPEM, _knownData, badSignature);
         Assert.IsFalse(verificationSuccess);
      }

      [Test]
      public async Task Stream_Signing_Matches_Regular_Signing()
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "CryptoLib_Tests", "Assets", "clarity_by_sigi_sagi.jpg");
         using var stream = File.Open(sampleFile, FileMode.Open);

         byte[] streamSignature = await _sut.SignStreamAsync(_knownPrivatePEM, stream, stream.Length, 60000, Maybe<Func<double, Task>>.None);

         stream.Seek(0, SeekOrigin.Begin);
         byte[] fileBytes = new byte[stream.Length];
         stream.Read(fileBytes, 0, (int)stream.Length);
         byte[] regularSignature = _sut.Sign(_knownPrivatePEM, fileBytes);
         
         Assert.AreEqual(regularSignature, streamSignature);
      }
   }
}
