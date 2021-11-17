using Crypter.Contracts.Responses;
using Crypter.Web.Models;
using Crypter.Web.Services;
using Crypter.Web.Services.API;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Web.Pages
{
   public partial class SearchBase : ComponentBase
   {
      [Inject]
      IJSRuntime JSRuntime { get; set; }

      [Inject]
      NavigationManager NavigationManager { get; set; }

      [Inject]
      IAuthenticationService AuthenticationService { get; set; }

      [Inject]
      IUserService UserService { get; set; }

      protected UserSearchParams SearchParams = new();
      protected UserSearchResponse SearchResults;

      protected override async Task OnInitializedAsync()
      {
         await JSRuntime.InvokeVoidAsync("setPageTitle", "Crypter - User Search");

         if (AuthenticationService.User == null)
         {
            NavigationManager.NavigateTo("/");
            return;
         }

         await base.OnInitializedAsync();
         
         await PerformSearchAsync();
      }

      protected async Task PerformSearchAsync()
      {
         ParseSearchParamsFromUri();

         if (string.IsNullOrEmpty(SearchParams.Query) || string.IsNullOrEmpty(SearchParams.Type))
         {
            return;
         }

         await JSRuntime.InvokeVoidAsync("setPageUrl", "/user/search?query=" + SearchParams.Query + "&type=" + SearchParams.Type + "&page=" + SearchParams.Page);
         var (_, response) = await UserService.GetUserSearchResultsAsync(SearchParams);
         SearchResults = response;

         if (SearchResults.Total > SearchParams.Results)
         {
            await SetActivePageAsync();
         }
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
      }

      protected async Task SetActivePageAsync()
      {
         await JSRuntime.InvokeVoidAsync("setActivePage", SearchParams.Page);
      }

      protected void GoToPage(string pageurl)
      {
         NavigationManager.NavigateTo(pageurl, true);
      }
   }
}
