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
using Crypter.CryptoLib.Services;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Test.CryptoLib_Tests.Services_Tests
{
   [TestFixture]
   public class SimpleEncryptionService_Tests
   {
      private byte[] _knownKey;
      private byte[] _knownIV;
      private byte[] _knownPlaintext;
      private byte[] _knownCiphertext;
      private string _knownStringPlaintext;
      private byte[] _knownStringCiphertext;
      private SimpleEncryptionService _sut;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _knownKey = new byte[] {
            0x41, 0x73, 0xc0, 0xd2, 0xe7, 0x1a, 0xe5, 0x4f,
            0xe1, 0x90, 0x83, 0x8f, 0x2e, 0x5a, 0xc7, 0xfc
         };

         _knownIV = new byte[]
         {
            0x5e, 0xdd, 0xed, 0x1a, 0x92, 0xa4, 0x89, 0x31,
            0x81, 0xb6, 0xa3, 0x47, 0xf6, 0xed, 0x8a, 0x6a
         };

         _knownPlaintext = new byte[]
         {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f
         };

         _knownCiphertext = new byte[]
         {
            0x38, 0x1c, 0xda, 0x9d, 0x68, 0xe7, 0xbf, 0x4b,
            0x96, 0x13, 0x3f, 0xc8, 0x35, 0xbb, 0x52, 0x35,
            0x33, 0x4a, 0x81, 0x02, 0xdc, 0x7d, 0x61, 0x2d,
            0x2e, 0x5b, 0x9f, 0xfb, 0x52, 0xfc, 0x35, 0xc9
         };

         _knownStringPlaintext = "foo";

         _knownStringCiphertext = new byte[]
         {
            0x5e, 0x72, 0xb7, 0x93, 0x61, 0xef, 0xb4, 0x41,
            0x93, 0x17, 0x38, 0xce, 0x34, 0xbb, 0x51, 0x37
         };

         _sut = new SimpleEncryptionService();
      }

      [Test]
      public void Encryption_Is_Predictable()
      {
         var newCiphertext = _sut.Encrypt(_knownKey, _knownIV, _knownPlaintext);
         Assert.AreEqual(_knownCiphertext, newCiphertext);
      }

      [Test]
      public void Decryption_Is_Predictable()
      {
         var knownKey = new byte[] {
            0x41, 0x73, 0xc0, 0xd2, 0xe7, 0x1a, 0xe5, 0x4f,
            0xe1, 0x90, 0x83, 0x8f, 0x2e, 0x5a, 0xc7, 0xfc
         };

         var knownIV = new byte[]
         {
            0x5e, 0xdd, 0xed, 0x1a, 0x92, 0xa4, 0x89, 0x31,
            0x81, 0xb6, 0xa3, 0x47, 0xf6, 0xed, 0x8a, 0x6a
         };

         var knownPlaintext = new byte[]
         {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f
         };

         var knownCiphertext = new byte[]
         {
            0x38, 0x1c, 0xda, 0x9d, 0x68, 0xe7, 0xbf, 0x4b,
            0x96, 0x13, 0x3f, 0xc8, 0x35, 0xbb, 0x52, 0x35,
            0x33, 0x4a, 0x81, 0x02, 0xdc, 0x7d, 0x61, 0x2d,
            0x2e, 0x5b, 0x9f, 0xfb, 0x52, 0xfc, 0x35, 0xc9
         };

         var sut = new SimpleEncryptionService();
         var newPlaintext = sut.Decrypt(knownKey, knownIV, knownCiphertext);

         Assert.AreEqual(knownPlaintext, newPlaintext);
      }

      [Test]
      public void Encryption_Provides_Unique_IV()
      {
         var (firstCiphertext, firstRandomIV) = _sut.Encrypt(_knownKey, _knownPlaintext);
         var (secondCiphertext, secondRandomIV) = _sut.Encrypt(_knownKey, _knownPlaintext);

         var matchesFirstCiphertext = _sut.Encrypt(_knownKey, firstRandomIV, _knownPlaintext);

         Assert.AreEqual(16, firstRandomIV.Length);
         Assert.AreEqual(16, secondRandomIV.Length);
         Assert.AreNotEqual(firstCiphertext, secondCiphertext);
         Assert.AreEqual(firstCiphertext, matchesFirstCiphertext);
      }

      [Test]
      public void Encryption_Works_On_String()
      {
         var newCiphertext = _sut.Encrypt(_knownKey, _knownIV, _knownStringPlaintext);
         Assert.AreEqual(_knownStringCiphertext, newCiphertext);
      }

      [Test]
      public void Decryption_Works_On_String()
      {
         var newString = _sut.DecryptToString(_knownKey, _knownIV, _knownStringCiphertext);
         Assert.AreEqual(_knownStringPlaintext, newString);
      }

      [Test]
      public async Task Stream_Encryption_Matches_Regular_Encryption()
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "CryptoLib_Tests", "Assets", "clarity_by_sigi_sagi.jpg");
         using var stream = File.Open(sampleFile, FileMode.Open);

         var partitionedCiphertextResult = await _sut.EncryptStreamAsync(_knownKey, _knownIV, stream, stream.Length, 60000, Maybe<Func<double, Task>>.None);

         int encryptedLength = partitionedCiphertextResult.Sum(x => x.Length);
         byte[] joinedResult = new byte[encryptedLength];

         int bytesCopied = 0;
         foreach (var part in partitionedCiphertextResult)
         {
            part.CopyTo(joinedResult, bytesCopied);
            bytesCopied += part.Length;
         }

         stream.Seek(0, SeekOrigin.Begin);
         byte[] wholePlaintext = new byte[stream.Length];
         stream.Read(wholePlaintext, 0, (int)stream.Length);
         byte[] regularCiphertextResult = _sut.Encrypt(_knownKey, _knownIV, wholePlaintext);

         Assert.AreEqual(regularCiphertextResult, joinedResult);
      }
   }
}
