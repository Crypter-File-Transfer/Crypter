﻿/*
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
using Crypter.Common.Primitives;
using Crypter.Web.Shared.Modal.Template;
using Crypter.Web.Shared.Transfer;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public partial class UploadFileTransferModalBase : ComponentBase
   {
      [Inject]
      protected IUserKeysService UserKeysService { get; set; }

      [Parameter]
      public string InstanceId { get; set; }

      [Parameter]
      public Maybe<string> RecipientUsername { get; set; }

      [Parameter]
      public Maybe<PEMString> RecipientX25519PublicKey { get; set; }

      [Parameter]
      public EventCallback ModalClosedCallback { get; set; }

      protected ModalBehavior ModalBehaviorRef { get; set; }

      protected UploadFileTransfer UploadComponent;

      protected bool IsSenderDefined = false;
      protected string SenderX25519PrivateKey;
      protected int RequestedExpirationHours;
      protected bool UseCompression;

      public void Open()
      {
         IsSenderDefined = UserKeysService.X25519PrivateKey.IsSome;

         SenderX25519PrivateKey = UserKeysService.X25519PrivateKey.Match(
            () => default,
            key => key.Value);

         ModalBehaviorRef.Open();
      }

      public async Task CloseAsync()
      {
         UploadComponent.Recycle();
         await ModalClosedCallback.InvokeAsync();
         ModalBehaviorRef.Close();
      }
   }
}
