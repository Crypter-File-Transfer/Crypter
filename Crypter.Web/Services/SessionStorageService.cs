using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
    public interface ISessionStorageService
    {
        Task<T> GetItem<T>(string key);
        Task SetItem<T>(string key, T value);
        Task RemoveItem(string key);
    }

    public class SessionStorageService : ISessionStorageService
    {
        private IJSRuntime _jSRuntime;

        public SessionStorageService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task<T> GetItem<T>(string key)
        {
            var json = await _jSRuntime.InvokeAsync<string>("sessionStorage.getItem", key);

            if (json == null)
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        public async Task SetItem<T>(string key, T value)
        {
            await _jSRuntime.InvokeVoidAsync("sessionStorage.setItem", key, JsonSerializer.Serialize(value));
        }

        public async Task RemoveItem(string key)
        {
            await _jSRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);
        }
    }
}
