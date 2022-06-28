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

using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IBrowserDownloadFileService
   {
      Task DownloadFileAsync(string fileName, string contentType, List<byte[]> fileByteParts);
      Task ResetDownloadAsync();
   }

   public class BrowserDownloadFileService : IBrowserDownloadFileService
   {
      private readonly IJSRuntime _jsRuntime;

      private const string _initializeBufferFunctionName = "window.Crypter.DownloadFile.InitializeBuffer";
      private const string _insertBufferFunctionName = "window.Crypter.DownloadFile.InsertBuffer";
      private const string _downloadFunctionName = "window.Crypter.DownloadFile.Download";
      private const string _resetDownloadFunctionName = "window.Crypter.DownloadFile.ResetDownload";

      public BrowserDownloadFileService(IJSRuntime jsRuntime)
      {
         _jsRuntime = jsRuntime;
      }

      public async Task DownloadFileAsync(string fileName, string contentType, List<byte[]> fileByteParts)
      {
         await _jsRuntime.InvokeVoidAsync(_resetDownloadFunctionName);
         await _jsRuntime.InvokeVoidAsync(_initializeBufferFunctionName, fileByteParts.Count);

         foreach (var part in fileByteParts)
         {
            await _jsRuntime.InvokeVoidAsync(_insertBufferFunctionName, part);
         }

         await _jsRuntime.InvokeVoidAsync(_downloadFunctionName, fileName, contentType);
      }

      public async Task ResetDownloadAsync()
      {
         await _jsRuntime.InvokeVoidAsync(_resetDownloadFunctionName);
      }
   }
}
