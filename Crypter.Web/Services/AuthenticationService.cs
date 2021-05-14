using Crypter.Web.Models;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Crypter.Contracts.Requests.Registered;

namespace Crypter.Web.Services
{

    public interface IAuthenticationService
    {
        User User { get; }
        Task Initialize();
        Task Login(string username, string password, string authenticationUrl);
        Task Logout();
    }
    public class AuthenticationService : IAuthenticationService
    {
        private IHttpService _httpService;
        private NavigationManager _navigationManager;
        private ISessionStorageService _sessionStorageService;

        public User User { get; private set; }

        public AuthenticationService(
            IHttpService httpService,
            NavigationManager navigationManager,
            ISessionStorageService sessionStorageService
        ) {
            _httpService = httpService;
            _navigationManager = navigationManager;
            _sessionStorageService = sessionStorageService;
        }

        public async Task Initialize()
        {
            User = await _sessionStorageService.GetItem<User>("user");
        }

        public async Task Login(string username, string password, string authenticationUrl)
        {
            User = await _httpService.Post<User>(authenticationUrl, new AuthenticateUserRequest(username, password));
            await _sessionStorageService.SetItem("user", User);
        }

        public async Task Logout()
        {
            User = null;
            await _sessionStorageService.RemoveItem("user");
            _navigationManager.NavigateTo("/");
        }
    }
}
