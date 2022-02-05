﻿/*
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

using Crypter.Contracts.Features.User.Search;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
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
      ILocalStorageService LocalStorage { get; set; }

      [Inject]
      IUserApiService UserService { get; set; }

      protected UserSearchParams SearchParams = new();
      protected UserSearchResponse SearchResults;

      protected override async Task OnInitializedAsync()
      {
         if (!LocalStorage.HasItem(StoredObjectType.UserSession))
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
         if (string.IsNullOrEmpty(SearchParams.Query) || string.IsNullOrEmpty(SearchParams.Type))
         {
            return;
         }

         await JSRuntime.InvokeVoidAsync("Crypter.SetPageUrl", "/user/search?query=" + SearchParams.Query + "&type=" + SearchParams.Type + "&page=" + SearchParams.Page);
         var maybeResults = await UserService.GetUserSearchResultsAsync(SearchParams);
         await maybeResults.DoRightAsync(async right => 
            {
               SearchResults = right;

               if (SearchResults.Total > SearchParams.Results)
               {
                  await SetActivePageAsync();
               }
            });
      }

      protected void ParseSearchParamsFromUri()
      {
         var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("query", out var query))
         {
            SearchParams.Query = query.First();
         }

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("type", out var queryType))
         {
            SearchParams.Type = queryType.First();
         }

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("page", out var pageNum))
         {
            SearchParams.Page = int.Parse(pageNum.First());
            SearchParams.Index = (SearchParams.Page - 1) * SearchParams.Results;
         }

         StateHasChanged();
      }

      protected async Task SetActivePageAsync()
      {
         await JSRuntime.InvokeVoidAsync("Crypter.SetActivePage", SearchParams.Page);
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

      public void Dispose()
      {
         NavigationManager.LocationChanged -= HandleLocationChanged;
         GC.SuppressFinalize(this);
      }
   }
}
