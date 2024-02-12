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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Web.Helpers;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Crypter.Web.Pages.Authenticated;

public partial class Search : IDisposable
{
    [Inject] private ICrypterApiClient CrypterApiService { get; init; } = null!;

    [Inject] private IUserContactsService UserContactsService { get; init; } = null!;

    private bool _loading = true;
    private string _sessionUsernameLowercase = string.Empty;
    private readonly UserSearchParameters _searchParameters = new UserSearchParameters(string.Empty, 0, 20);
    protected List<ContactSearchResult> SearchResults = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        bool isLoggedIn = await UserSessionService.IsLoggedInAsync();
        if (!isLoggedIn)
        {
            return;
        }
        
        NavigationManager.LocationChanged += HandleLocationChanged;
        _sessionUsernameLowercase = UserSessionService.Session.Match(
            () => string.Empty,
            x => x.Username.ToLower());
        ParseSearchParamsFromUri();
        _loading = false;
        await PerformSearchAsync();
    }

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrEmpty(_searchParameters.Keyword))
        {
            return;
        }

        SearchResults = await CrypterApiService.User.GetUserSearchResultsAsync(_searchParameters)
            .BindAsync<List<UserSearchResult>, List<ContactSearchResult>>(async searchResults =>
            {
                bool[] contactLookupTasks = await Task.WhenAll(
                    searchResults.Select(x =>
                            UserContactsService.IsContactAsync(x.Username))
                        .ToList());

                return contactLookupTasks.Zip(searchResults.Select(x => x))
                    .Select(x => new ContactSearchResult(x.Second.Username, x.Second.Alias, x.First))
                    .ToList();
            }).SomeOrDefaultAsync([]);
    }

    private void OnSearchClicked()
    {
        NavigationManager.NavigateTo($"/user/search?query={_searchParameters.Keyword}");
    }

    private void ParseSearchParamsFromUri()
    {
        string? query = NavigationManager.GetQueryParameter("query");
        if (!string.IsNullOrEmpty(query))
        {
            _searchParameters.Keyword = query;
        }

        StateHasChanged();
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (e.Location.Contains("/user/search"))
        {
            ParseSearchParamsFromUri();
            InvokeAsync(async () =>
            {
                await PerformSearchAsync();
                StateHasChanged();
            });
        }
    }

    private async Task AddContactAsync(string contactUsername)
    {
        bool contactAdded = (await UserContactsService.AddContactAsync(contactUsername))
            .IsRight;

        if (contactAdded)
        {
            ContactSearchResult? addedContact = SearchResults
                .FirstOrDefault(x => x.Username == contactUsername);

            if (addedContact is not null)
            {
                addedContact.IsContact = true;
            }
        }

        StateHasChanged();
    }

    private static string GetDisplayName(string username, string alias)
    {
        return string.IsNullOrEmpty(alias)
            ? username
            : $"{alias} ({username})";
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
    }

    protected class ContactSearchResult : UserSearchResult
    {
        public bool IsContact { get; set; }

        public ContactSearchResult(string username, string alias, bool isContact) : base(username, alias)
        {
            IsContact = isContact;
        }
    }
}
