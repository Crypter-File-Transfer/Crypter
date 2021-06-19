using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Web.Models;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IUserService
   {
      Task<(HttpStatusCode HttpStatus, UserAuthenticateResponse Response)> AuthenticateUserAsync(AuthenticateUserRequest loginRequest);
      Task<(HttpStatusCode HttpStatus, UserAuthenticationRefreshResponse Response)> RefreshAuthenticationAsync();
      Task<(HttpStatusCode HttpStatus, UserRegisterResponse Response)> RegisterUserAsync(RegisterUserRequest registerRequest);
      Task<(HttpStatusCode HttpStatus, UserPublicProfileResponse Response)> GetUserPublicProfileAsync(string username);
      Task<(HttpStatusCode HttpStatus, UserSettingsResponse Response)> GetUserSettingsAsync();
      Task<(HttpStatusCode HttpStatus, UpdateUserPrivacyResponse Response)> UpdateUserPrivacyAsync(UpdateUserPrivacyRequest request);
      Task<(HttpStatusCode HttpStatus, UserSentMessagesResponse Response)> GetUserSentMessagesAsync();
      Task<(HttpStatusCode HttpStatus, UserSentFilesResponse Response)> GetUserSentFilesAsync();
      Task<(HttpStatusCode HttpStatus, UserReceivedMessagesResponse Response)> GetUserReceivedMessagesAsync();
      Task<(HttpStatusCode HttpStatus, UserReceivedFilesResponse Response)> GetUserReceivedFilesAsync();
   }

   public class UserService : IUserService
   {
      private readonly string BaseUserUrl;
      private readonly IHttpService HttpService;

      public UserService(AppSettings appSettings, IHttpService httpService)
      {
         BaseUserUrl = $"{appSettings.ApiBaseUrl}/user";
         HttpService = httpService;
      }

      public async Task<(HttpStatusCode, UserAuthenticateResponse)> AuthenticateUserAsync(AuthenticateUserRequest loginRequest)
      {
         var url = $"{BaseUserUrl}/authenticate";
         return await HttpService.Post<UserAuthenticateResponse>(url, loginRequest);
      }

      public async Task<(HttpStatusCode, UserAuthenticationRefreshResponse)> RefreshAuthenticationAsync()
      {
         var url = $"{BaseUserUrl}/authenticate/refresh";
         return await HttpService.Get<UserAuthenticationRefreshResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserRegisterResponse)> RegisterUserAsync(RegisterUserRequest registerRequest)
      {
         var url = $"{BaseUserUrl}/register";
         return await HttpService.Post<UserRegisterResponse>(url, registerRequest);
      }

      public async Task<(HttpStatusCode, UserPublicProfileResponse)> GetUserPublicProfileAsync(string username)
      {
         var url = $"{BaseUserUrl}/{username}";
         return await HttpService.Get<UserPublicProfileResponse>(url);
      }

      public async Task<(HttpStatusCode, UserSettingsResponse)> GetUserSettingsAsync()
      {
         var url = $"{BaseUserUrl}/settings";
         return await HttpService.Get<UserSettingsResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UpdateUserPrivacyResponse)> UpdateUserPrivacyAsync(UpdateUserPrivacyRequest request)
      {
         var url = $"{BaseUserUrl}/update-privacy";
         return await HttpService.Post<UpdateUserPrivacyResponse>(url, request, true);
      }

      public async Task<(HttpStatusCode, UserSentMessagesResponse)> GetUserSentMessagesAsync()
      {
         var url = $"{BaseUserUrl}/sent/messages";
         return await HttpService.Get<UserSentMessagesResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserSentFilesResponse)> GetUserSentFilesAsync()
      {
         var url = $"{BaseUserUrl}/sent/files";
         return await HttpService.Get<UserSentFilesResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserReceivedMessagesResponse)> GetUserReceivedMessagesAsync()
      {
         var url = $"{BaseUserUrl}/received/messages";
         return await HttpService.Get<UserReceivedMessagesResponse>(url, true);
      }

      public async Task<(HttpStatusCode, UserReceivedFilesResponse)> GetUserReceivedFilesAsync()
      {
         var url = $"{BaseUserUrl}/received/files";
         return await HttpService.Get<UserReceivedFilesResponse>(url, true);
      }
   }
}
