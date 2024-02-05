/*
 * Copyright (C) 2024 Crypter File Transfer
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

using System;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Primitives;
using Crypter.Web.Shared.Modal.Template;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.Modal;

public partial class PasswordModal
{
    [Inject] private IUserSessionService UserSessionService { get; set; } = null!;

    [Parameter] public required EventCallback<bool> ModalClosedCallback { get; set; }

    private ModalBehavior ModalBehaviorRef { get; set; } = null!;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _passwordTestFailed;

    public void Open()
    {
        _username = UserSessionService.Session.Match(
            () => string.Empty,
            x => x.Username);

        ModalBehaviorRef.Open();
    }

    private async Task CloseAsync(bool success)
    {
        await ModalClosedCallback.InvokeAsync(success);
        ModalBehaviorRef.Close();
    }

    private async Task<bool> TestPasswordAsync()
    {
        if (!Password.TryFrom(_password, out Password password))
        {
            return false;
        }

        return await UserSessionService.TestPasswordAsync(password);
    }

    private async Task OnSubmitClickedAsync()
    {
        if (await TestPasswordAsync())
        {
            await CloseAsync(true);
        }

        _passwordTestFailed = true;
    }

    private async Task OnCancelClickedAsync()
    {
        await CloseAsync(false);
    }
}
