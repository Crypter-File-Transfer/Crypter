using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Web.Models;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Services
{
   public interface IDownloadService
   {
      Task<(HttpStatusCode HttpStatus, GenericSignatureResponse Response)> DownloadMessageSignatureAsync(GenericSignatureRequest downloadRequest, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, GenericCiphertextResponse Response)> DownloadMessageCiphertextAsync(GenericCiphertextRequest downloadRequest, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, GenericSignatureResponse Response)> DownloadFileSignatureAsync(GenericSignatureRequest downloadRequest, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, GenericCiphertextResponse Response)> DownloadFileCiphertextAsync(GenericCiphertextRequest downloadRequest, bool withAuthentication);
   }
   public class DownloadService : IDownloadService
   {
      private readonly string BaseDownloadUrl;
      private readonly IHttpService HttpService;

      public DownloadService(AppSettings appSettings, IHttpService httpService)
      {
         BaseDownloadUrl = $"{appSettings.ApiBaseUrl}/download";
         HttpService = httpService;
      }

      public async Task<(HttpStatusCode, GenericSignatureResponse)> DownloadMessageSignatureAsync(GenericSignatureRequest downloadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseDownloadUrl}/message/signature/auth"
            : $"{BaseDownloadUrl}/message/signature/anon";
         return await HttpService.Post<GenericSignatureResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<(HttpStatusCode, GenericCiphertextResponse)> DownloadMessageCiphertextAsync(GenericCiphertextRequest downloadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseDownloadUrl}/message/ciphertext/auth"
            : $"{BaseDownloadUrl}/message/ciphertext/anon";
         return await HttpService.Post<GenericCiphertextResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<(HttpStatusCode, GenericSignatureResponse)> DownloadFileSignatureAsync(GenericSignatureRequest downloadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseDownloadUrl}/file/signature/auth"
            : $"{BaseDownloadUrl}/file/signature/anon";
         return await HttpService.Post<GenericSignatureResponse>(url, downloadRequest, withAuthentication);
      }

      public async Task<(HttpStatusCode, GenericCiphertextResponse)> DownloadFileCiphertextAsync(GenericCiphertextRequest downloadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseDownloadUrl}/file/ciphertext/auth"
            : $"{BaseDownloadUrl}/file/ciphertext/anon";
         return await HttpService.Post<GenericCiphertextResponse>(url, downloadRequest, withAuthentication);
      }
   }
}
