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

using Crypter.ClientServices.Transfer;
using Crypter.ClientServices.Transfer.Handlers;
using Crypter.Common.Monads;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Transfer
{
   public partial class UploadFileTransferBase : UploadTransferBase
   {
      protected IBrowserFile SelectedFile;

      // Settings
      protected const int MaxFileCount = 1;
      protected long MaxFileSizeBytes = 0;

      // UI
      protected bool ShowProgressBar = false;
      protected double ProgressPercent = 0.0;
      protected string DropClass = string.Empty;

      // Strings
      private const string _dropzoneDrag = "dropzone-drag";
      private const string _noFileSelected = "No file selected.";

      protected override void OnInitialized()
      {
         MaxFileSizeBytes = UploadSettings.MaxUploadSizeMB * (long)Math.Pow(2, 20);
      }

      protected void HandleDragEnter()
      {
         DropClass = _dropzoneDrag;
      }

      protected void HandleDragLeave()
      {
         DropClass = string.Empty;
      }

      protected void HandleFileInputChange(InputFileChangeEventArgs e)
      {
         DropClass = string.Empty;
         ErrorMessage = string.Empty;

         var file = e.File;

         if (file is null)
         {
            ErrorMessage = _noFileSelected;
            return;
         }

         if (file.Size > MaxFileSizeBytes)
         {
            ErrorMessage = $"The max file size is {UploadSettings.MaxUploadSizeMB} MB.";
            return;
         }

         SelectedFile = file;
      }

      protected async Task OnEncryptClicked()
      {
         if (SelectedFile is null)
         {
            ErrorMessage = _noFileSelected;
            return;
         }

         EncryptionInProgress = true;
         ErrorMessage = string.Empty;

         await SetProgressMessage("Reading file");
         using Stream fileStream = SelectedFile.OpenReadStream(SelectedFile.Size);
         using UploadFileHandler fileUploader = TransferHandlerFactory.CreateUploadFileHandler(fileStream, SelectedFile.Name, SelectedFile.Size, SelectedFile.ContentType, ExpirationHours, UseCompression);

         SetHandlerUserInfo(fileUploader);

         var compressionProgressTask = Maybe<Func<double, Task>>.From(SetCompressionProgress);
         var encryptionProgressTask = Maybe<Func<double, Task>>.From(SetEncryptionProgress);
         var showUploadingMessage = Maybe<Func<Task>>.From(async () => {
            ShowProgressBar = false;
            await SetProgressMessage(_uploadingLiteral);
         });
         await fileUploader.PrepareMemoryStream(compressionProgressTask);
         var uploadResponse = await fileUploader.UploadAsync(encryptionProgressTask, showUploadingMessage);

         HandleUploadResponse(uploadResponse);

         Recycle();
         EncryptionInProgress = false;
      }

      protected async Task SetCompressionProgress(double percentComplete)
      {
         if (UploadStatusMessage != _compressingLiteral)
         {
            UploadStatusMessage = _compressingLiteral;
         }

         await SetProgressBar(percentComplete);
      }

      protected async Task SetEncryptionProgress(double percentComplete)
      {
         if (UploadStatusMessage != _encryptingLiteral)
         {
            UploadStatusMessage = _encryptingLiteral;
         }

         await SetProgressBar(percentComplete);
      }

      protected async Task SetProgressBar(double percentComplete)
      {
         ShowProgressBar = true;
         ProgressPercent = percentComplete;
         StateHasChanged();

         int delay = percentComplete == 1.0
            ? 400
            : 5;

         await Task.Delay(delay);
      }

      protected async Task SetProgressMessage(string message)
      {
         UploadStatusMessage = message;
         StateHasChanged();
         await Task.Delay(400);
      }

      public void Recycle()
      {
         SelectedFile = null;
      }
   }
}
