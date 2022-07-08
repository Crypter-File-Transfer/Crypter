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

using Crypter.ClientServices.Services;
using Crypter.Common.Monads;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.Test.ClientServices_Tests
{
   [TestFixture]
   public class CompressionService_Tests
   {
      private Func<double, Task> _progressFunc;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _progressFunc = async (double progress) => await Task.FromResult(0);
      }

      [Test]
      public async Task Compression_Works()
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "ClientServices_Tests", "Assets", "Saturn.jpg");
         using var originalFileStream = File.Open(sampleFile, FileMode.Open);

         CompressionService sut = new CompressionService();
         using MemoryStream compressedStream = await sut.CompressStreamAsync(originalFileStream, originalFileStream.Length, 4096, _progressFunc);

         Assert.AreEqual(44730, originalFileStream.Length);
         Assert.AreEqual(44316, compressedStream.Length);
      }

      [TestCase(0, 32_613_752)]
      [TestCase(1, 10_748_492)]
      [TestCase(2, 10_237_438)]
      [TestCase(3, 10_104_537)]
      [TestCase(4, 10_227_675)]
      [TestCase(5, 9_867_018)]
      [TestCase(6, 9_833_433)]
      [TestCase(7, 9_826_090)]
      [TestCase(8, 9_816_687)]
      [TestCase(9, 9_815_417)]
      public async Task Compression_Levels_Work(int compressionLevel, int compressedFileSize)
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "ClientServices_Tests", "Assets", "WindowsCodecsRaw.dll");
         using var originalFileStream = File.Open(sampleFile, FileMode.Open);

         CompressionService sut = new CompressionService();
         using MemoryStream compressedStream = await sut.CompressStreamAsync(originalFileStream, originalFileStream.Length, 4096, _progressFunc, compressionLevel);

         Assert.AreEqual(32_608_744, originalFileStream.Length);
         Assert.AreEqual(compressedFileSize, compressedStream.Length);
      }

      [Test]
      public async Task Compression_Copy_To_Works()
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "ClientServices_Tests", "Assets", "Saturn.jpg");
         using var originalFileStream = File.Open(sampleFile, FileMode.Open);

         CompressionService sut = new CompressionService();
         using MemoryStream compressedStream = await sut.CompressStreamAsync(originalFileStream);

         Assert.AreEqual(44730, originalFileStream.Length);
         Assert.AreEqual(44316, compressedStream.Length);
      }

      [Test]
      public async Task Decompression_Works()
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "ClientServices_Tests", "Assets", "Saturn.jpg.gz");
         using var compressedFileStream = File.Open(sampleFile, FileMode.Open);

         CompressionService sut = new CompressionService();
         using MemoryStream uncompressedStream = await sut.DecompressStreamAsync(compressedFileStream, compressedFileStream.Length, 4096, Maybe<Func<double, Task>>.None);

         Assert.AreEqual(44316, compressedFileStream.Length);
         Assert.AreEqual(44730, uncompressedStream.Length);
      }

      [TestCase("Saturn.jpg")]
      [TestCase("WindowsCodecsRaw.dll")]
      public async Task Compression_And_Decompression_Work_Together(string fileName)
      {
         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var sampleFile = Path.Combine(directory, "ClientServices_Tests", "Assets", fileName);
         using var originalFileStream = File.Open(sampleFile, FileMode.Open);

         CompressionService sut = new CompressionService();
         using MemoryStream compressedStream = await sut.CompressStreamAsync(originalFileStream, originalFileStream.Length, 4096, _progressFunc);
         using MemoryStream uncompressedStream = await sut.DecompressStreamAsync(compressedStream, compressedStream.Length, 4096, _progressFunc);

         Assert.AreEqual(originalFileStream.Length, uncompressedStream.Length);

         byte[] originalFileBytes = new byte[originalFileStream.Length];
         await originalFileStream.ReadAsync(originalFileBytes);

         byte[] uncompressedFileBytes = new byte[uncompressedStream.Length];
         await uncompressedStream.ReadAsync(uncompressedFileBytes);

         CollectionAssert.AreEqual(originalFileBytes, uncompressedFileBytes);
      }
   }
}
