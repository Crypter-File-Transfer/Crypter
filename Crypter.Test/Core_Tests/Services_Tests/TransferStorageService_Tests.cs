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

using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Core.Services;
using Crypter.Core.Settings;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   internal class TransferStorageService_Tests
   {
      private TransferStorageService _sut;
      private string _storageLocation;

      [OneTimeSetUp]
      public void OneTimeSetup()
      {
         SetupService();
      }

      private void SetupService()
      {
         _storageLocation = OperatingSystem.IsLinux()
            ? "/home/runner/work/Crypter/crypter_files"
            : "C:\\crypter_files";

         TransferStorageSettings settings = new TransferStorageSettings
         {
            AllocatedGB = 1,
            Location = _storageLocation
         };
         IOptions<TransferStorageSettings> options = Options.Create(settings);
         _sut = new TransferStorageService(options);
      }

      [OneTimeTearDown]
      public void OneTimeTearDown()
      {
         Directory.Delete(_storageLocation, true);
      }

      [TestCase(TransferUserType.Anonymous)]
      [TestCase(TransferUserType.User)]
      public async Task Files_Can_Be_Saved_And_Read_Async(TransferUserType userType)
      {
         byte[] buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };
         Guid itemGuid = Guid.NewGuid();
         MemoryStream memoryStream = new MemoryStream(buffer);
         bool saveSuccess = await _sut.SaveTransferAsync(itemGuid, TransferItemType.File, userType, memoryStream, CancellationToken.None);
         Assert.IsTrue(saveSuccess);
         memoryStream.Dispose();

         Maybe<FileStream> fileStream = _sut.GetTransfer(itemGuid, TransferItemType.File, userType, true);
         Assert.IsTrue(fileStream.IsSome);
         fileStream.IfSome(async x =>
         {
            byte[] readBuffer = new byte[4];
            int bytesRead = await x.ReadAsync(readBuffer);
            Assert.AreEqual(4, bytesRead);
            Assert.AreEqual(buffer, readBuffer);
            x.Dispose();
         });
      }
   }
}
