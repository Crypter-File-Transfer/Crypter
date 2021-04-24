using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrypterAPI.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Crypter.Contracts.Requests.Anonymous;
using Crypter.Contracts.Responses.Anonymous;
using Crypter.Contracts.Enum;

namespace CrypterAPI.Controllers
{
    [Route("api/message")]
    [Produces("application/json")]
    //[ApiController]
    public class TextUploadItemsController : ControllerBase
    {
        private readonly CrypterDB Db;
        private readonly string BaseSaveDirectory;

        public TextUploadItemsController(CrypterDB db, IConfiguration configuration)
        {
            Db = db;
            BaseSaveDirectory = configuration["EncryptedFileStore"];
        }

        // POST: crypter.dev/api/message
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostTextUploadItem([FromBody] AnonymousMessageUploadRequest body)
        {
            await Db.Connection.OpenAsync();

            var newText = new TextUploadItem();
            newText.CipherText = body.CipherText;
            newText.Signature = body.Signature;

            await newText.InsertAsync(Db, BaseSaveDirectory);

            var responseBody = new AnonymousUploadResponse(Guid.Parse(newText.ID), newText.ExpirationDate);
            return new JsonResult(responseBody);
        }

        // GET: crypter.dev/api/message/preview/{id}
        [HttpGet("preview/{id}")]
        public async Task<IActionResult> GetTextPreview(string id)
        {
            Guid guid = Guid.Empty;
            if (!Guid.TryParse(id, out guid))
            {
               var invalidResponseBody = new AnonymousMessagePreviewResponse(ResponseCode.InvalidRequest);
               return new BadRequestObjectResult(invalidResponseBody);
            }

            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(guid.ToString());
            if (result is null)
            {
               var notFoundResponseBody = new AnonymousMessagePreviewResponse(ResponseCode.NotFound);
               return new NotFoundObjectResult(notFoundResponseBody);
            }

            var responseBody = new AnonymousMessagePreviewResponse(result.Size, result.Created, result.ExpirationDate);
            return new OkObjectResult(responseBody);
        }

        // GET: crypter.dev/api/message/actual/{guid}
        [HttpGet("actual/{id}")]
        public async Task<IActionResult> GetTextUploadActual(string id)
        {
            Guid guid = Guid.Empty;
            if (!Guid.TryParse(id, out guid))
            {
                var invalidResponseBody = new AnonymousDownloadResponse(ResponseCode.InvalidRequest);
                return new BadRequestObjectResult(invalidResponseBody);
            }
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //read file bytes and convert to base64 string
            string cipherText = Convert.ToBase64String(System.IO.File.ReadAllBytes(result.CipherTextPath));
            var responseBody = new AnonymousDownloadResponse(cipherText);
            //TODO: Apply decryption key to remove server-side encryption

            //return the encrypted message bytes
            return new OkObjectResult(responseBody);
        }

        // GET: crypter.dev/api/message/signature/{guid}
        [HttpGet("signature/{id}")]
        public async Task<IActionResult> GetTextUploadSig(string id)
        {
            Guid guid = Guid.Empty;
            if (!Guid.TryParse(id, out guid))
            {
                var invalidResponseBody = new AnonymousDownloadResponse(ResponseCode.InvalidRequest);
                return new BadRequestObjectResult(invalidResponseBody);
            }
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //read and return signature using SignaturePath
            string signature = System.IO.File.ReadAllText(result.SignaturePath);
            var responseBody = new AnonymousSignatureResponse(signature);
            //return the signature
            return new OkObjectResult(responseBody); 
        }

        // PUT: crypter.dev/api/message/signature/{guid}
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("signature/{id}")]
        public async Task<IActionResult> PutTextUploadItem(string id, [FromBody] TextUploadItem body)
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
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

        // DELETE: crypter.dev/api/message/{guid}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTextUploadItem(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            await result.DeleteAsync(Db);
            return new OkResult();
        }
    }
}