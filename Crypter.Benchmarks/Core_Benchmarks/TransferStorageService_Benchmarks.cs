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
using Crypter.Benchmarks.Config;
using Crypter.Benchmarks.Core_Benchmarks.Old_Implementations;
using Crypter.Common.Enums;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Benchmarks.Core_Benchmarks
{
   /*
   [MemoryDiagnoser]
   [Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
   [RankColumn]
   [Config(typeof(AntiVirusFriendlyConfig))]
   public class TransferStorageService_Benchmarks
   {
      private TransferStorageService _sut;
      private TransferStorageService_SingleFile _sutSingleFileAlternative;
      private const string _encryptedFileStore = "C:\\CrypterFiles\\Benchmarks";
      private TransferStorageParameters _iterationTransferParams;
      private TransferStorageParameters _persistentTransferParams;
      private Guid _persistentItemId;

      [GlobalSetup]
      public void GlobalSetup()
      {
         TransferStorageSettings singleFileSettings = new TransferStorageSettings
         {
            AllocatedGB = 1,
            Location = $"{_encryptedFileStore}\\single"
         };
         IOptions<TransferStorageSettings> singleFileOptions = Options.Create(singleFileSettings);
         _sutSingleFileAlternative = new TransferStorageService_SingleFile(singleFileOptions);

         TransferStorageSettings multiFileSettings = new TransferStorageSettings
         {
            AllocatedGB = 1,
            Location = $"{_encryptedFileStore}\\multi"
         };
         IOptions<TransferStorageSettings> multifileOptions = Options.Create(multiFileSettings);
         _sut = new TransferStorageService(multifileOptions);

         var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
         string fileLocation = Path.Combine(directory, "Assets", "WindowsCodecsRaw.dll");

         byte[] fileBytes = File.ReadAllBytes(fileLocation);
         int blobPosition = 0;

         List<byte[]> chunkedFileBytes = new List<byte[]>();
         while (blobPosition < fileBytes.Length)
         {
            if (blobPosition + 65536 < fileBytes.Length)
            {
               chunkedFileBytes.Add(fileBytes[blobPosition..(blobPosition += 65536)]);
            }
            else
            {
               chunkedFileBytes.Add(fileBytes[blobPosition..^1]);
               break;
            }
         }

         _persistentItemId = Guid.NewGuid();
         byte[] header = new byte[] { 0x01, 0x02, 0x03, 0x04 };
         _persistentTransferParams = new TransferStorageParameters(_persistentItemId, TransferItemType.File, TransferUserType.Anonymous, header, chunkedFileBytes);
      }

      [IterationSetup]
      public void IterationSetup()
      {
         _iterationTransferParams = new TransferStorageParameters(Guid.NewGuid(), TransferItemType.File, TransferUserType.Anonymous, _persistentTransferParams.Header, _persistentTransferParams.Ciphertext);
         _sutSingleFileAlternative.SaveTransferAsync(_persistentTransferParams, CancellationToken.None).Wait();
         _sut.SaveTransferAsync(_persistentTransferParams, CancellationToken.None).Wait();
      }

      [IterationCleanup]
      public void IterationCleanup()
      {
         Directory.Delete(_encryptedFileStore, true);
      }

      [Benchmark]
      [WarmupCount(1)]
      [IterationCount(5)]
      public async Task WriteSingleFileAsync()
      {
         await _sut.SaveTransferAsync(_iterationTransferParams, CancellationToken.None);
      }

      [Benchmark]
      [WarmupCount(1)]
      [IterationCount(5)]
      public async Task ReadSingleFileAsync()
      {
         await _sut.ReadTransferAsync(_persistentItemId, TransferItemType.File, TransferUserType.Anonymous, CancellationToken.None);
      }

      [Benchmark]
      [WarmupCount(1)]
      [IterationCount(5)]
      public async Task WriteMultipleFilesAsync()
      {
         await _sutSingleFileAlternative.SaveTransferAsync(_iterationTransferParams, CancellationToken.None);
      }

      [Benchmark]
      [WarmupCount(1)]
      [IterationCount(5)]
      public async Task ReadMultipleFilesAsync()
      {
         await _sutSingleFileAlternative.ReadTransferAsync(_persistentItemId, TransferItemType.File, TransferUserType.Anonymous, CancellationToken.None);
      }
   }
   */
}
