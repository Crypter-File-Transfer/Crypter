/*
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
using Crypter.Contracts.Features.User.Search;
using Crypter.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class SearchBase : ComponentBase, IDisposable
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      [Inject]
      protected IUserContactsService UserContactsService { get; set; }

      [Inject]
      private ISessionService SessionService { get; set; }

      protected UserSearchParameters SearchParameters = new("");
      protected UserSearchResponse SearchResults;

      protected override async Task OnInitializedAsync()
      {
         if (!SessionService.IsLoggedIn)
         {
            NavigationManager.NavigateTo("/");
            return;
         }
         NavigationManager.LocationChanged += HandleLocationChanged;
         ParseSearchParamsFromUri();
         await PerformSearchAsync();
      }

      protected async Task PerformSearchAsync()
      {
         if (string.IsNullOrEmpty(SearchParameters.Keyword))
         {
            return;
         }

         int page = 1 + (SearchParameters.Index / SearchParameters.Count);
         await JSRuntime.InvokeVoidAsync("Crypter.SetPageUrl", "/user/search?query=" + SearchParameters.Keyword + "&page=" + page);
         var maybeResults = await CrypterApiService.GetUserSearchResultsAsync(SearchParameters);
         await maybeResults.DoRightAsync(async right => 
         {
            SearchResults = right;

            if (SearchResults.Total > SearchParameters.Count)
            {
               await SetActivePageAsync(page);
            }
         });
      }

      protected void ParseSearchParamsFromUri()
      {
         var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("query", out var query))
         {
            SearchParameters.Keyword = query.First();
         }

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("page", out var pageNum))
         {
            int page = int.Parse(pageNum.First());
            SearchParameters.Index = (page - 1) * SearchParameters.Count;
         }

         StateHasChanged();
      }

      protected async Task SetActivePageAsync(int page)
      {
         await JSRuntime.InvokeVoidAsync("Crypter.SetActivePage", page);
      }

      protected void GoToPage(string pageUrl)
      {
         NavigationManager.NavigateTo(pageUrl);
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

      protected async Task AddContactAsync(Guid user)
      {
         await UserContactsService.AddContactAsync(user);
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
   }
}
