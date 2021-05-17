using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
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
        private readonly ISessionStorageService _sessionStorageService;

        public HttpService(
            HttpClient httpClient,
            NavigationManager navigationManager,
            ISessionStorageService sessionStorageService
        ) {
            _httpClient = httpClient;
            _navigationManager = navigationManager;
            _sessionStorageService = sessionStorageService;
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
            var user = await _sessionStorageService.GetItem<User>("user");
            if (user != null && withAuthorization)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);
            }

            using var response = await _httpClient.SendAsync(request);

            // Todo
            // We shouldn't log the user out just because they received an unauthorized response.
            // The user could be legitimately logged in and just tried going to a stale URL.
            // Send the user to an "unauthorized" page instead.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _navigationManager.NavigateTo("logout");
                return default;
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                throw new Exception(error["message"]);
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }
    }
}
