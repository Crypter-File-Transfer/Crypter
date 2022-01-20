/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Shared.Transfer
{
    public partial class TransferSettingsBase : ComponentBase
    {
        private int _requestedExpirationHours;

        [Parameter]
        public int RequestedExpirationHours
        {
            get => _requestedExpirationHours;
            set
            {
                if (_requestedExpirationHours == value) return;
                if (value < 1) value = 1;
                if (value > 24) value = 24;
                _requestedExpirationHours = value;

                RequestedExpirationHoursChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<int> RequestedExpirationHoursChanged { get; set; }

        protected override void OnInitialized()
        {
            RequestedExpirationHours = 24;
        }
    }
}
