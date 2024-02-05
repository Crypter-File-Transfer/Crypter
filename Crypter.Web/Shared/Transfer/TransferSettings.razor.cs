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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.Transfer;

public partial class TransferSettings
{
    private const int MinExpirationHours = 1;
    private const int MaxExpirationHours = 24;
    private const int DefaultExpirationHours = 24;

    private string _expirationInput = string.Empty;

    [Parameter] public int ExpirationHours { get; set; }

    [Parameter] public EventCallback<int> ExpirationHoursChanged { get; set; }

    protected override Task OnParametersSetAsync()
    {
        switch (ExpirationHours)
        {
            case > MaxExpirationHours:
                _expirationInput = MaxExpirationHours.ToString();
                return ExpirationHoursChanged.InvokeAsync(MaxExpirationHours);
            case < MinExpirationHours:
                _expirationInput = MinExpirationHours.ToString();
                return ExpirationHoursChanged.InvokeAsync(DefaultExpirationHours);
            default:
                _expirationInput = ExpirationHours.ToString();
                return Task.CompletedTask;
        }
    }

    private Task OnExpirationHoursChanged(string value)
    {
        if (!int.TryParse(value, out int parsedValue))
        {
            ExpirationHours = DefaultExpirationHours;
            _expirationInput = DefaultExpirationHours.ToString();
            return ExpirationHoursChanged.InvokeAsync(DefaultExpirationHours);
        }

        if (ExpirationHours == parsedValue)
        {
            return Task.CompletedTask;
        }

        return parsedValue switch
        {
            > MaxExpirationHours => ExpirationHoursChanged.InvokeAsync(MaxExpirationHours),
            < MinExpirationHours => ExpirationHoursChanged.InvokeAsync(MinExpirationHours),
            _ => ExpirationHoursChanged.InvokeAsync(parsedValue)
        };
    }
}
