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
        Task<T> Get<T>(string uri, bool withAuthorization = false);
        Task<T> Post<T>(string uri, object value, bool withAuthorization = false);
    }

    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;
        private readonly ILocalStorageService _localStorageService;

        public HttpService(
            HttpClient httpClient,
            NavigationManager navigationManager,
            ILocalStorageService localStorageService
        ) {
            _httpClient = httpClient;
            _navigationManager = navigationManager;
            _localStorageService = localStorageService;
        }

        public async Task<T> Get<T>(string uri, bool withAuthorization)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            return await SendRequest<T>(request, withAuthorization);
        }

        public async Task<T> Post<T>(string uri, object value, bool withAuthorization)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = JsonContent.Create(value)
            };
            return await SendRequest<T>(request, withAuthorization);
        }

        private async Task<T> SendRequest<T>(HttpRequestMessage request, bool withAuthorization)
        {
            
            if (withAuthorization)
            {
                var user = await _localStorageService.GetItem<User>("user");
                if (user == null)
                {
                    return HandleMissingUser<T>();
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception)
            {
                return default;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return HandleMissingUser<T>();
            }

            T content = await response.Content.ReadFromJsonAsync<T>();
            response.Dispose();

            return content;
        }

        private T HandleMissingUser<T>()
        {
            _navigationManager.NavigateTo("/", true);
            return default;
        }
    }
}
