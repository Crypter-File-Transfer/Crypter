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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.Common.Streams
{
   public static class StreamCopy
   {
      public static async Task<MemoryStream> StreamToMemoryStream(Stream stream, long streamLength, int bufferSize, Maybe<Func<double, Task>> progressFunc)
      {
         await progressFunc.IfSomeAsync(async func => await func.Invoke(0.0));

         MemoryStream outputStream = new MemoryStream();

         long totalBytesRead = 0;
         int bytesRead = 0;
         do
         {
            byte[] buffer = new byte[bufferSize];
            bytesRead = await stream.ReadAsync(buffer);
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
         outputStream.Position = 0;
         return outputStream;
      }

      public static async Task<List<byte[]>> StreamToBytePartitions(Stream stream, long streamLength, int partitionSize, Maybe<Func<double, Task>> progressFunc)
      {
         await progressFunc.IfSomeAsync(async func => await func.Invoke(0.0));

         List<byte[]> outputBytes = new List<byte[]>();

         int bytesRead = 0;
         do
         {
            byte[] buffer = new byte[partitionSize];
            bytesRead = await stream.ReadAsync(buffer);

            if (bytesRead > 0)
            {
               if (bytesRead < partitionSize)
               {
                  outputBytes.Add(buffer[..bytesRead]);
               }
               else
               {
                  outputBytes.Add(buffer);
               }

               await progressFunc.IfSomeAsync(async func =>
               {
                  double progress = (double)bytesRead / streamLength;
                  await func.Invoke(progress);
               });
            }
         } while (bytesRead > 0);

         await progressFunc.IfSomeAsync(async func => await func.Invoke(1.0));
         return outputBytes;
      }
   }
}
