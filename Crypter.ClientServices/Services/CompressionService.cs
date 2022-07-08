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

using Crypter.ClientServices.Interfaces;
using Crypter.Common.Monads;
using ICSharpCode.SharpZipLib.GZip;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class CompressionService : ICompressionService
   {
      public async Task<MemoryStream> CompressStreamAsync(Stream stream, long streamLength, int bufferSize, Func<double, Task> progressFunc, int compressionLevel = 6)
      {
         await progressFunc.Invoke(0.0);

         MemoryStream outputStream = new MemoryStream();
         using GZipOutputStream gzipStream = new GZipOutputStream(outputStream, bufferSize);
         gzipStream.IsStreamOwner = false;
         gzipStream.SetLevel(compressionLevel);

         long totalBytesRead = 0;
         int bytesRead = 0;
         do
         {
            byte[] buffer = new byte[bufferSize];
            bytesRead = await stream.ReadAsync(buffer);
            totalBytesRead += bytesRead;

            if (bytesRead > 0)
            {
               await gzipStream.WriteAsync(buffer.AsMemory()[..bytesRead]);
               double progress = (double)totalBytesRead / streamLength;
               await progressFunc.Invoke(progress);
            }
         } while (bytesRead > 0);

         await gzipStream.FlushAsync();
         gzipStream.Finish();
         await progressFunc.Invoke(1.0);

         outputStream.Position = 0;
         return outputStream;
      }

      public async Task<MemoryStream> CompressStreamAsync(Stream stream, int compressionLevel = 6)
      {
         MemoryStream outputStream = new MemoryStream();
         using GZipOutputStream gzipStream = new GZipOutputStream(outputStream);
         gzipStream.IsStreamOwner = false;
         gzipStream.SetLevel(compressionLevel);

         await stream.CopyToAsync(gzipStream);

         await gzipStream.FlushAsync();
         gzipStream.Finish();

         outputStream.Position = 0;
         return outputStream;
      }

      public async Task<MemoryStream> DecompressStreamAsync(Stream compressedStream, long streamLength, int bufferSize, Maybe<Func<double, Task>> progressFunc)
      {
         await progressFunc.IfSomeAsync(async func => await func.Invoke(0.0));

         MemoryStream outputStream = new MemoryStream();
         using GZipInputStream gzipStream = new GZipInputStream(compressedStream, bufferSize);
         gzipStream.IsStreamOwner = false;

         long totalBytesRead = 0;
         int bytesRead = 0;
         do
         {
            byte[] buffer = new byte[bufferSize];
            bytesRead = await gzipStream.ReadAsync(buffer);
            totalBytesRead += bytesRead;

            if (bytesRead > 0)
            {
               await outputStream.WriteAsync(buffer.AsMemory()[..bytesRead]);
               await progressFunc.IfSomeAsync(async func =>
               {
                  double progress = (double)totalBytesRead / streamLength;
                  await func.Invoke(progress);
               });
            }
         } while (bytesRead > 0);

         await progressFunc.IfSomeAsync(async func => await func.Invoke(1.0));
         return outputStream;
      }
   }
}
