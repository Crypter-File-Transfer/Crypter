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

using System.Threading.Tasks;
using Crypter.Web.Shared.Modal.Template;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.Modal;

public partial class BasicModal
{
    private string _subject;
    private string _message;
    private string _primaryButtonText;
    private string _secondaryButtonText;

    private Maybe<EventCallback<bool>> _modalClosedCallback;
    private ModalBehavior _modalBehaviorRef;

    public void Open(string subject, string message, string primaryButtonText, Maybe<string> secondaryButtonText,
        Maybe<EventCallback<bool>> modalClosedCallback)
    {
        _subject = subject;
        _message = message;
        _primaryButtonText = primaryButtonText;
        _secondaryButtonText = secondaryButtonText.SomeOrDefault(string.Empty);
        _modalClosedCallback = modalClosedCallback;

        _modalBehaviorRef.Open();
    }

    private async Task CloseAsync(bool modalClosedInTheAffirmative)
    {
        await _modalClosedCallback.IfSomeAsync(async x => await x.InvokeAsync(modalClosedInTheAffirmative));
        _modalBehaviorRef.Close();
    }
}
