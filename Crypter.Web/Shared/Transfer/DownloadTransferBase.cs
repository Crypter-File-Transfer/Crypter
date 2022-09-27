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
using Crypter.ClientServices.Transfer;
using Crypter.Common.Enums;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Microsoft.AspNetCore.Components;
using System;
using System.Text;

namespace Crypter.Web.Shared.Transfer
{
   public partial class DownloadTransferBase : ComponentBase
   {
      [Inject]
      private NavigationManager NavigationManager { get; set; }

      [Inject]
      protected IUserKeysService UserKeysService { get; set; }

      [Inject]
      protected TransferHandlerFactory TransferHandlerFactory { get; set; }

      [Parameter]
      public string TransferHashId { get; set; }

      [Parameter]
      public TransferUserType UserType { get; set; }

      protected bool FinishedLoading = false;
      protected bool ItemFound = false;
      protected bool DecryptionInProgress = false;
      protected bool DecryptionComplete = false;
      protected string ErrorMessage = string.Empty;
      protected string DecryptionStatusMessage = string.Empty;

      protected bool SpecificRecipient = false;
      protected string SenderUsername = string.Empty;
      protected DateTime Created = DateTime.MinValue;
      protected DateTime Expiration = DateTime.MinValue;

      protected const string _downloadingLiteral = "Downloading";
      protected const string _decompressingLiteral = "Decompressing";
      protected const string _decryptingLiteral = "Decrypting";
      protected const string _verifyingLiteral = "Verifying";

      protected Maybe<PEMString> ValidateAndDecodeUserProvidedDecryptionKey()
      {
         int hashLocation = NavigationManager.Uri.IndexOf('#');
         string decryptionKey = NavigationManager.Uri.Substring(hashLocation + 1);
         Console.WriteLine(decryptionKey);

         if (Base64String.TryFrom(decryptionKey, out Base64String validatedBase64EncryptionKey))
         {
            byte[] decodedKey = Convert.FromBase64String(validatedBase64EncryptionKey.Value);
            string pemFormattedKey = Encoding.UTF8.GetString(decodedKey);
            if (PEMString.TryFrom(pemFormattedKey, out PEMString validDecryptionKey))
            {
               return validDecryptionKey;
            }
         }

         return Maybe<PEMString>.None;
      }
   }
}
