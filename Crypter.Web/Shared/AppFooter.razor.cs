/*
 * Copyright (C) 2025 Crypter File Transfer
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

using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.Version;
using Crypter.Common.Services;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
    public partial class AppFooter
    {
        [Inject] private IJSRuntime JsRuntime { get; set; } = null!;

        [Inject] private ICrypterApiClient CrypterApiService { get; init; } = null!;

        [Inject] private IVersionService VersionService { get; set; } = null!;

        private string _apiVersion = string.Empty;
        private string _apiVersionUrl = string.Empty;
        private string _clientVersion = string.Empty;
        private string _clientVersionUrl = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            Maybe<VersionResponse> response = await CrypterApiService.ApiVersion.GetApiVersionAsync();
            _apiVersion = response.Match("1.0.0", x => x.IsRelease ? x.ProductVersion : $"SHA: {x.VersionHash?[..7] ?? "???"}");
            _apiVersionUrl = response.Match(string.Empty, x => x.VersionSystemUrl);
            _clientVersion = VersionService.IsRelease ? VersionService.ProductVersion : $"SHA: {VersionService.VersionHash?[..7] ?? "???"}";
            _clientVersionUrl = VersionService.VersionUrl;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JsRuntime.InvokeVoidAsync("Crypter.InitVersionPopover");
            await base.OnAfterRenderAsync(firstRender);
        }

    }
}
