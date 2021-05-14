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
        Task Login(string username, string password);
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

        public async Task Login(string username, string password)
        {
            User = await _httpService.Post<User>("https://localhost:5001/api/user/authenticate", new AuthenticateUserRequest(username, password));
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
