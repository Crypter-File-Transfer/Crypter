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

using Crypter.Common.Client.Interfaces.HttpClients;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Contracts.Features.Users;
using Crypter.Web.Helpers;
using Crypter.Web.Pages.Authenticated.Base;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyMonads;

namespace Crypter.Web.Pages
{
   public partial class SearchBase : AuthenticatedPageBase, IDisposable
   {
      [Inject]
      protected ICrypterApiClient CrypterApiService { get; set; }

      [Inject]
      protected IUserContactsService UserContactsService { get; set; }

      protected bool Loading = true;
      protected string SessionUsernameLowercase = string.Empty;
      protected UserSearchParameters SearchParameters;
      protected List<ContactSearchResult> SearchResults;

      protected override async Task OnInitializedAsync()
      {
         await base.OnInitializedAsync();
         bool isLoggedIn = await UserSessionService.IsLoggedInAsync();
         if (!isLoggedIn)
         {
            return;
         }

         SearchParameters = new UserSearchParameters(string.Empty, 0, 20);
         NavigationManager.LocationChanged += HandleLocationChanged;
         SessionUsernameLowercase = UserSessionService.Session.Match(
            () => string.Empty,
            x => x.Username.ToLower());
         ParseSearchParamsFromUri();
         Loading = false;
         await PerformSearchAsync();
      }

      protected async Task PerformSearchAsync()
      {
         if (string.IsNullOrEmpty(SearchParameters.Keyword))
         {
            return;
         }

         SearchResults = await CrypterApiService.User.GetUserSearchResultsAsync(SearchParameters)
            .BindAsync<List<UserSearchResult>, List<ContactSearchResult>>(async searchResults =>
            {
               bool[] contactLookupTasks = await Task.WhenAll(
                  searchResults.Select(x =>
                     UserContactsService.IsContactAsync(x.Username))
                  .ToList());
               
               return contactLookupTasks.Zip(searchResults.Select(x => x))
                  .Select(x => new ContactSearchResult(x.Second.Username, x.Second.Alias, x.First))
                  .ToList();
            }).SomeOrDefaultAsync(null);
      }

      protected void OnSearchClicked()
      {
         NavigationManager.NavigateTo($"/user/search?query={SearchParameters.Keyword}");
      }

      protected void ParseSearchParamsFromUri()
      {
         string query = NavigationManager.GetQueryParameter("query");
         if (!string.IsNullOrEmpty(query))
         {
            SearchParameters.Keyword = query;
         }

         StateHasChanged();
      }

      protected void HandleLocationChanged(object sender, LocationChangedEventArgs e)
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

      protected async Task AddContactAsync(string contactUsername)
      {
         bool contactAdded = (await UserContactsService.AddContactAsync(contactUsername))
            .IsRight;

         if (contactAdded)
         {
            ContactSearchResult addedContact = SearchResults
               .Where(x => x.Username == contactUsername)
               .FirstOrDefault();

            if (addedContact is not null)
            {
               addedContact.IsContact = true;
            }
         }

         StateHasChanged();
      }

      protected static string GetDisplayName(string username, string alias)
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
}
