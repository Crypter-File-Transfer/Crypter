using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrypterAPI.Models;
using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Crypter.Contracts.Requests.Anonymous;
using Crypter.Contracts.Responses.Anonymous;
using Crypter.Contracts.Enum;
using System.IO; 
namespace CrypterAPI.Controllers
{
    [Route("api/file")]
    [Produces("application/json")]
    //[ApiController]
    public class FileUploadItemsController : ControllerBase
    {
        private readonly CrypterDB Db;
        private readonly string BaseSaveDirectory;

        public FileUploadItemsController(CrypterDB db, IConfiguration configuration)
        {
            Db = db;
            BaseSaveDirectory = configuration["EncryptedFileStore"];
        }

        // POST: crypter.dev/api/file
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostFileUploadItem([FromBody] AnonymousFileUploadRequest body)
        {
            await Db.Connection.OpenAsync();

            var newFile = new FileUploadItem();
            newFile.FileName = body.Name;
            newFile.CipherText = body.CipherText;
            newFile.Signature = body.Signature;

            await newFile.InsertAsync(Db, BaseSaveDirectory);

            var responseBody = new AnonymousUploadResponse(Guid.Parse(newFile.ID), newFile.ExpirationDate);
            return new JsonResult(responseBody);

        }

        // GET: crypter.dev/api/file/preview/{guid}
        [HttpGet("preview/{id}")]
        public async Task<IActionResult> GetFilePreview(string id)
        {
            Guid guid = Guid.Empty;
            if (!Guid.TryParse(id, out guid))
            {
               var invalidResponseBody = new AnonymousFilePreviewResponse(ResponseCode.InvalidRequest);
               return new BadRequestObjectResult(invalidResponseBody);
            }

            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(guid.ToString());
            if (result is null)
            {
               var notFoundResponseBody = new AnonymousFilePreviewResponse(ResponseCode.NotFound);
               return new NotFoundObjectResult(notFoundResponseBody);
            }

            var responseBody = new AnonymousFilePreviewResponse(result.FileName, result.Size, result.Created, result.ExpirationDate);
            return new OkObjectResult(responseBody);
        }

        // POST: crypter.dev/api/file/actual
        [HttpPost("actual")]
        public async Task<IActionResult> GetFileUploadActual([FromBody] AnonymousFileDownloadRequest body)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(body.Id.ToString());
            if (result is null)
                return new NotFoundResult();
            //read file bytes and convert to base64 string
            string cipherText = Convert.ToBase64String(System.IO.File.ReadAllBytes(result.CipherTextPath));
            var responseBody = new AnonymousDownloadResponse(cipherText);
            //TODO: Apply decryption key to remove server-side encryption

            //return the encrypted file bytes
            return new OkObjectResult(responseBody);
        }

        // GET: crypter.dev/api/file/signature/{guid}
        [HttpGet("signature/{id}")]
        public async Task<IActionResult> GetFileUploadSig(string id)
        {
            Guid guid = Guid.Empty;
            if (!Guid.TryParse(id, out guid))
            {
                var invalidResponseBody = new AnonymousDownloadResponse(ResponseCode.InvalidRequest);
                return new BadRequestObjectResult(invalidResponseBody);
            }
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(guid.ToString());
            if (result is null)
                return new NotFoundResult();
            //read and return signature using Signature Path
            string signature = System.IO.File.ReadAllText(result.SignaturePath);
            var responseBody = new AnonymousSignatureResponse(signature);
            //return the signature
            return new OkObjectResult(responseBody); 
        }

        // PUT: crypter.dev/api/file/{guid}
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFileUploadItem(string id, [FromBody] FileUploadItem body)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //update fields
            result.UserID = body.UserID;
            result.FileName = body.FileName;
            result.Size = body.Size;
            result.SignaturePath = body.SignaturePath;
            result.Created = body.Created;
            result.ExpirationDate = body.ExpirationDate;
            result.CipherTextPath = body.CipherTextPath;
            await result.UpdateAsync(Db);
            return new OkObjectResult(result);

        }

        // DELETE: crypter.dev/api/file/{guid}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFileUploadItem(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            await result.DeleteAsync(Db);
            return new OkResult();
        }

    }
}