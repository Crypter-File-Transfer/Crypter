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

using System.Collections.Specialized;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Contracts.Features.UserSettings;
using Crypter.Web.Helpers;
using Crypter.Web.Models.Forms;
using EasyMonads;
using Microsoft.AspNetCore.Components;

namespace Crypter.Web.Pages;

public partial class Verify
{
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [Inject] private ICrypterApiClient CrypterApiService { get; set; } = null!;

    private bool _emailVerificationInProgress = true;
    private bool _emailVerificationSuccess;

    protected override async Task OnInitializedAsync()
    {
        if (TryParseVerificationParamsFromUri(out EmailVerificationParameters parameters))
        {
            await VerifyEmailAddressAsync(parameters);
        }
        
        _emailVerificationInProgress = false;
    }

    private bool TryParseVerificationParamsFromUri(out EmailVerificationParameters parameters)
    {
        parameters = new EmailVerificationParameters();
        NameValueCollection queryParameters = NavigationManager.GetQueryParameters();
        
        string? code = queryParameters["code"];
        string? signature = queryParameters["signature"];
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        parameters.Code = code;
        parameters.Signature = signature;

        return true;
    }

    private async Task VerifyEmailAddressAsync(EmailVerificationParameters parameters)
    {
        _emailVerificationSuccess = await CrypterApiService.UserSetting.VerifyUserEmailAddressAsync(
                new VerifyEmailAddressRequest(parameters.Code, parameters.Signature))
            .MatchAsync(leftOrNeither: false, right: _ => true);
    }
}
