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

using Crypter.CryptoLib.Crypto;
using Crypter.CryptoLib.Enums;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace Crypter.Test.CryptoLib_Tests
{
   [TestFixture]
   public class AES_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void KeyGen_Produces_256_Bit_Keys()
      {
         var key = AES.GenerateKey(AesKeySize._256);
         Assert.AreEqual(32, key.GetKey().Length);
      }

      [Test]
      public void KeyGen_Produces_192_Bit_Keys()
      {
         var key = AES.GenerateKey(AesKeySize._192);
         Assert.AreEqual(24, key.GetKey().Length);
      }

      [Test]
      public void KeyGen_Produces_128_Bit_Keys()
      {
         var key = AES.GenerateKey(AesKeySize._128);
         Assert.AreEqual(16, key.GetKey().Length);
      }

      [Test]
      public void KeyGen_Produces_Unique_Keys()
      {
         var key1 = AES.GenerateKey(AesKeySize._256);
         var key2 = AES.GenerateKey(AesKeySize._256);
         Assert.AreNotEqual(key1.GetKey(), key2.GetKey());
      }

      [Test]
      public void IV_Generation_Produces_128_Bit_Values()
      {
         var iv = AES.GenerateIV();
         Assert.AreEqual(16, iv.Length);
      }

      [Test]
      public void IV_Generation_Produces_Unique_Values()
      {
         var iv1 = AES.GenerateIV();
         var iv2 = AES.GenerateIV();
         Assert.AreNotEqual(iv1, iv2);
      }

      [Test]
      public void Encryption_Is_Predictable()
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

         var cipher = new AES();
         cipher.Initialize(knownKey, knownIV, true);
         var newCiphertext = cipher.ProcessFinal(knownPlaintext);
         Assert.AreEqual(knownCiphertext, newCiphertext);
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

         var cipher = new AES();
         cipher.Initialize(knownKey, knownIV, false);
         var newPlaintext = cipher.ProcessFinal(knownCiphertext);
         Assert.AreEqual(knownPlaintext, newPlaintext);
      }

      [Test]
      public void Encryption_And_Decryption_Work_On_A_Large_File()
      {
         var knownKey = new byte[]
         {
                0x41, 0x73, 0xc0, 0xd2, 0xe7, 0x1a, 0xe5, 0x4f,
                0xe1, 0x90, 0x83, 0x8f, 0x2e, 0x5a, 0xc7, 0xfc
         };

         var knownIV = new byte[]
         {
                0x5e, 0xdd, 0xed, 0x1a, 0x92, 0xa4, 0x89, 0x31,
                0x81, 0xb6, 0xa3, 0x47, 0xf6, 0xed, 0x8a, 0x6a
         };

         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "CryptoLib_Tests", "Assets", "clarity_by_sigi_sagi.jpg");
         using var sampleStream = File.Open(sampleFile, FileMode.Open);
         byte[] plaintext = new byte[sampleStream.Length];
         sampleStream.Read(plaintext, 0, (int)sampleStream.Length);

         var cipherForEncryption = new AES();
         cipherForEncryption.Initialize(knownKey, knownIV, true);
         var cipherText = cipherForEncryption.ProcessFinal(plaintext);

         var cipherForDecryption = new AES();
         cipherForDecryption.Initialize(knownKey, knownIV, false);
         var decrypted = cipherForDecryption.ProcessFinal(cipherText);

         Assert.AreEqual(plaintext, decrypted);
      }

      [Test]
      public void Encryption_Can_Be_Chunked()
      {
         var knownKey = new byte[]
         {
                0x41, 0x73, 0xc0, 0xd2, 0xe7, 0x1a, 0xe5, 0x4f,
                0xe1, 0x90, 0x83, 0x8f, 0x2e, 0x5a, 0xc7, 0xfc
         };

         var knownIV = new byte[]
         {
                0x5e, 0xdd, 0xed, 0x1a, 0x92, 0xa4, 0x89, 0x31,
                0x81, 0xb6, 0xa3, 0x47, 0xf6, 0xed, 0x8a, 0x6a
         };

         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "CryptoLib_Tests", "Assets", "clarity_by_sigi_sagi.jpg");
         using var sampleStream = File.Open(sampleFile, FileMode.Open);
         var fileSize = sampleStream.Length;

         int processedPlaintextBytes = 0;
         int currentCiphertextSize = 0;
         int chunkSize = 1048576;
         var cipherForEncryption = new AES();
         cipherForEncryption.Initialize(knownKey, knownIV, true);
         List<byte> plaintext = new List<byte>((int)fileSize);
         List<byte> ciphertext = new List<byte>(cipherForEncryption.GetOutputSize((int)fileSize));
         while (processedPlaintextBytes + chunkSize < fileSize)
         {
            var plaintextChunk = new byte[chunkSize];
            sampleStream.Read(plaintextChunk, 0, plaintextChunk.Length);
            plaintext.InsertRange(processedPlaintextBytes, plaintextChunk);
            var ciphertextChunk = cipherForEncryption.ProcessChunk(plaintextChunk);

            ciphertext.InsertRange(currentCiphertextSize, ciphertextChunk);
            processedPlaintextBytes += chunkSize;
            currentCiphertextSize += ciphertextChunk.Length;
         }

         var finalPlaintextChunk = new byte[fileSize - processedPlaintextBytes];
         sampleStream.Read(finalPlaintextChunk, 0, finalPlaintextChunk.Length);
         plaintext.InsertRange(processedPlaintextBytes, finalPlaintextChunk);
         var finalCiphertextChunk = cipherForEncryption.ProcessFinal(finalPlaintextChunk);
         ciphertext.InsertRange(currentCiphertextSize, finalCiphertextChunk);

         var cipherForDecryption = new AES();
         cipherForDecryption.Initialize(knownKey, knownIV, false);
         var decrypted = cipherForDecryption.ProcessFinal(ciphertext.ToArray());

         Assert.AreEqual(plaintext.ToArray(), decrypted);
      }

      /// <summary>
      /// This test is interesting since the ciphertext will contain some padding, making
      ///  the ciphertext larger than the original plaintext. This makes it impossible to know
      ///  exactly how large a decrypted plaintext will be.
      /// 
      /// A List<byte> is initialized with a capacity equal to GetOutputSize. It's important
      ///  to remember the difference between a List's "capacity" and it's "count". When the
      ///  List is converted to an Array, only the elements in the List are converted. Excess
      ///  capacity is ignored.
      /// </summary>
      [Test]
      public void Decryption_Can_Be_Chunked()
      {
         var knownKey = new byte[]
         {
                0x41, 0x73, 0xc0, 0xd2, 0xe7, 0x1a, 0xe5, 0x4f,
                0xe1, 0x90, 0x83, 0x8f, 0x2e, 0x5a, 0xc7, 0xfc
         };

         var knownIV = new byte[]
         {
                0x5e, 0xdd, 0xed, 0x1a, 0x92, 0xa4, 0x89, 0x31,
                0x81, 0xb6, 0xa3, 0x47, 0xf6, 0xed, 0x8a, 0x6a
         };

         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "CryptoLib_Tests", "Assets", "clarity_by_sigi_sagi.jpg");
         using var sampleStream = File.Open(sampleFile, FileMode.Open);
         byte[] plaintext = new byte[sampleStream.Length];
         sampleStream.Read(plaintext);

         var cipherForEncryption = new AES();
         cipherForEncryption.Initialize(knownKey, knownIV, true);
         var ciphertext = cipherForEncryption.ProcessFinal(plaintext);

         int processedCiphertextBytes = 0;
         int currentPlaintextSize = 0;
         int chunkSize = 1048576;
         var cipherForDecryption = new AES();
         cipherForDecryption.Initialize(knownKey, knownIV, false);
         List<byte> decrypted = new List<byte>(cipherForDecryption.GetOutputSize(ciphertext.Length));
         var outputSize = cipherForDecryption.GetOutputSize(ciphertext.Length);
         while (processedCiphertextBytes + chunkSize < ciphertext.Length)
         {
            var ciphertextChunk = ciphertext[processedCiphertextBytes..(processedCiphertextBytes + chunkSize)];
            var plaintextChunk = cipherForDecryption.ProcessChunk(ciphertextChunk);

            decrypted.InsertRange(currentPlaintextSize, plaintextChunk);
            processedCiphertextBytes += chunkSize;
            currentPlaintextSize += plaintextChunk.Length;
         }

         var finalCiphertextChunk = ciphertext[processedCiphertextBytes..ciphertext.Length];
         var finalPlaintextChunk = cipherForDecryption.ProcessFinal(finalCiphertextChunk);
         decrypted.InsertRange(currentPlaintextSize, finalPlaintextChunk);

         Assert.AreEqual(plaintext, decrypted.ToArray());
      }
   }
}
