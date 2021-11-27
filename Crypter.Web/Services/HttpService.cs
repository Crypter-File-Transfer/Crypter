using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IHttpService
   {
      Task<(HttpStatusCode HttpStatus, T Payload)> Get<T>(string uri, bool withAuthorization = false);
      Task<(HttpStatusCode HttpStatus, T Payload)> Post<T>(string uri, object value, bool withAuthorization = false);
   }

   public class HttpService : IHttpService
   {
      private readonly HttpClient HttpClient;
      private readonly NavigationManager NavigationManager;
      private readonly ILocalStorageService LocalStorage;

      public HttpService(
          HttpClient httpClient,
          NavigationManager navigationManager,
          ILocalStorageService localStorage
      )
      {
         HttpClient = httpClient;
         NavigationManager = navigationManager;
         LocalStorage = localStorage;
      }

      public async Task<(HttpStatusCode HttpStatus, T Payload)> Get<T>(string uri, bool withAuthorization)
      {
         var request = new HttpRequestMessage(HttpMethod.Get, uri);
         return await SendRequest<T>(request, withAuthorization);
      }

      public async Task<(HttpStatusCode HttpStatus, T Payload)> Post<T>(string uri, object value, bool withAuthorization)
      {
         var request = new HttpRequestMessage(HttpMethod.Post, uri)
         {
            Content = JsonContent.Create(value)
         };
         return await SendRequest<T>(request, withAuthorization);
      }

      private async Task<(HttpStatusCode HttpStatus, T Payload)> SendRequest<T>(HttpRequestMessage request, bool withAuthorization)
      {

         if (withAuthorization)
         {
            var token = (await LocalStorage.GetItem<UserSession>(StoredObjectType.UserSession))?.Token;
            if (string.IsNullOrEmpty(token))
            {
               return HandleMissingUser<T>();
            }
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
         }

         HttpResponseMessage response;
         try
         {
            response = await HttpClient.SendAsync(request);
         }
         catch (Exception)
         {
            return (HttpStatusCode.ServiceUnavailable, default);
         }

         if (response.StatusCode == HttpStatusCode.Unauthorized)
         {
            return HandleMissingUser<T>();
         }

         T content = await response.Content.ReadFromJsonAsync<T>();
         response.Dispose();

         return (response.StatusCode, content);
      }

      private (HttpStatusCode HttpStatus, T Payload) HandleMissingUser<T>()
      {
         NavigationManager.NavigateTo("/");
         return (HttpStatusCode.Unauthorized, default);
      }
   }
}
