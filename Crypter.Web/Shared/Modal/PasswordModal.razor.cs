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

using Crypter.Common.Client.Interfaces.Services;
using Crypter.Web.Shared.Modal.Template;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Crypter.Web.Shared.Modal
{
   public class PasswordModalBase : ComponentBase
   {
      [Inject]
      private IUserSessionService UserSessionService { get; set; }

      [Parameter]
      public EventCallback<bool> ModalClosedCallback { get; set; }

      protected ModalBehavior ModalBehaviorRef { get; set; }

      protected string Username;
      protected string Password;
      protected bool PasswordTestFailed;

      public void Open()
      {
         Username = UserSessionService.Session.Match(
            () => string.Empty,
            x => x.Username);

         ModalBehaviorRef.Open();
      }

      public async Task CloseAsync(bool success)
      {
         await ModalClosedCallback.InvokeAsync(success);
         ModalBehaviorRef.Close();
      }

      public async Task<bool> TestPasswordAsync()
      {
         if (!Common.Primitives.Password.TryFrom(Password, out var password))
         {
            return false;
         }

         return await UserSessionService.TestPasswordAsync(password);
      }

      public async Task OnSubmitClickedAsync()
      {
         if (await TestPasswordAsync())
         {
            await CloseAsync(true);
         }

         PasswordTestFailed = true;
      }

      public async Task OnCancelClickedAsync()
      {
         await CloseAsync(false);
      }
   }
}
