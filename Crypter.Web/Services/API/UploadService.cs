using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Web.Models;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Services.API
{
   public interface IUploadService
   {
      Task<(HttpStatusCode HttpStatus, GenericUploadResponse Response)> UploadMessageAsync(MessageUploadRequest uploadRequest, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, GenericUploadResponse Response)> UploadFileAsync(FileUploadRequest uploadRequest, bool withAuthentication);
   }

   public class UploadService : IUploadService
   {
      private readonly string BaseUploadUrl;
      private readonly IHttpService HttpService;

      public UploadService(AppSettings appSettings, IHttpService httpService)
      {
         BaseUploadUrl = $"{appSettings.ApiBaseUrl}/upload";
         HttpService = httpService;
      }

      public async Task<(HttpStatusCode, GenericUploadResponse)> UploadMessageAsync(MessageUploadRequest uploadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseUploadUrl}/message/auth"
            : $"{BaseUploadUrl}/message/anon";
         return await HttpService.Post<GenericUploadResponse>(url, uploadRequest, withAuthentication);
      }

      public async Task<(HttpStatusCode, GenericUploadResponse)> UploadFileAsync(FileUploadRequest uploadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseUploadUrl}/file/auth"
            : $"{BaseUploadUrl}/file/anon";
         return await HttpService.Post<GenericUploadResponse>(url, uploadRequest, withAuthentication);
      }
   }
}
