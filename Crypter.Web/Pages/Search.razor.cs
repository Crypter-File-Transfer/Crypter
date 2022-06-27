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
using Crypter.Common.Monads;
using Crypter.Contracts.Features.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class SearchBase : ComponentBase, IDisposable
   {
      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      protected ICrypterApiService CrypterApiService { get; set; }

      [Inject]
      protected IUserContactsService UserContactsService { get; set; }

      [Inject]
      protected IUserSessionService UserSessionService { get; set; }

      protected bool Loading;
      protected string SessionUsernameLowercase = string.Empty;
      protected UserSearchParameters SearchParameters;
      protected UserSearchResponse SearchResults;

      protected override async Task OnInitializedAsync()
      {
         Loading = true;
         if (!UserSessionService.LoggedIn)
         {
            NavigationManager.NavigateTo("/");
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

         await CrypterApiService.GetUserSearchResultsAsync(SearchParameters)
            .DoRightAsync(x =>
            {
               SearchResults = x;
            });
      }

      protected void OnSearchClicked()
      {
         NavigationManager.NavigateTo($"/user/search?query={SearchParameters.Keyword}");
      }

      protected void ParseSearchParamsFromUri()
      {
         var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

         if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("query", out var query))
         {
            SearchParameters.Keyword = query.First();
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
         await UserContactsService.AddContactAsync(contactUsername);
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
