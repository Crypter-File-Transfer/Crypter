using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Web.Models;
using System.Net;
using System.Threading.Tasks;

namespace Crypter.Web.Services.API
{
   public interface IDownloadService
   {
      Task<(HttpStatusCode HttpStatus, MessagePreviewResponse Response)> DownloadMessagePreviewAsync(GenericPreviewRequest downloadRequest, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, GenericSignatureResponse Response)> DownloadMessageSignatureAsync(GenericSignatureRequest downloadRequest, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, GenericCiphertextResponse Response)> DownloadMessageCiphertextAsync(GenericCiphertextRequest downloadRequest, bool withAuthentication);
      Task<(HttpStatusCode HttpStatus, FilePreviewResponse Response)> DownloadFilePreviewAsync(GenericPreviewRequest downloadRequest, bool withAuthentication);
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

      public async Task<(HttpStatusCode, MessagePreviewResponse)> DownloadMessagePreviewAsync(GenericPreviewRequest downloadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseDownloadUrl}/message/preview/auth"
            : $"{BaseDownloadUrl}/message/preview/anon";
         return await HttpService.Post<MessagePreviewResponse>(url, downloadRequest, withAuthentication);
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

      public async Task<(HttpStatusCode, FilePreviewResponse)> DownloadFilePreviewAsync(GenericPreviewRequest downloadRequest, bool withAuthentication)
      {
         var url = withAuthentication
            ? $"{BaseDownloadUrl}/file/preview/auth"
            : $"{BaseDownloadUrl}/file/preview/anon";
         return await HttpService.Post<FilePreviewResponse>(url, downloadRequest, withAuthentication);
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
