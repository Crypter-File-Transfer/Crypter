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

using BenchmarkDotNet.Attributes;
using Crypter.ClientServices.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.Benchmarks.ClientServices_Benchmarks
{
   [MemoryDiagnoser]
   [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
   [RankColumn]
   public class CompressionService_CompressionBufferBenchmark
   {
      private CompressionService _compressionService;
      private FileStream _fileStream;
      private Func<double, Task> _progressFunc;

      [Params(1_024, 4_096, 16_368, 84_999, 85_000, 340_000, 1_360_000)]
      public int BufferSize;

      [GlobalSetup]
      public void GlobalSetup()
      {
         _progressFunc = async (double progress) => await Task.FromResult(0);
      }

      [IterationSetup]
      public void IterationSetup()
      {
         _compressionService = new CompressionService();

         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         var filePath = Path.Combine(directory, "Assets", "WindowsCodecsRaw.dll");
         _fileStream = File.OpenRead(filePath);
      }

      [IterationCleanup]
      public void IterationCleanup()
      {
         _fileStream.Close();
      }

      [Benchmark]
      public async Task CompressWithVaryingBufferSize()
      {
         using MemoryStream compressedStream = await _compressionService.CompressStreamAsync(_fileStream, _fileStream.Length, BufferSize, _progressFunc);
      }
   }
}
