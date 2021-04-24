using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrypterAPI.Models;
using System;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Crypter.Contracts.Requests.Anonymous;
using Crypter.Contracts.Responses.Anonymous;
using Crypter.Contracts.Enum;

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
        // Probably not a use case for this GET
        // GET: crypter.dev/api/file
        [HttpGet]
        public async Task<IActionResult> GetFileUploadItems()
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.LatestItemsAsync();
            return new OkObjectResult(result);
        }

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

        // GET: crypter.dev/api/file/actual/{guid}
        [HttpGet("actual/{id}")]
        public async Task<IActionResult> GetFileUploadActual(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //obtain file path for actual encrypted file
            Console.WriteLine(result.CipherTextPath);
            //return the encrypted file 
            return new JsonResult(result.CipherTextPath);
        }

        // GET: crypter.dev/api/file/signature/{guid}
        [HttpGet("signature/{id}")]
        public async Task<IActionResult> GetFileUploadSig(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //obtain file path for the signature of the encrypted file
            Console.WriteLine(result.SignaturePath);
            //TODO: read and return signature
            string signature = System.IO.File.ReadAllText(result.SignaturePath);
            Console.WriteLine(signature);
            //Send signature in response-
            Dictionary<string, string> SigDict = new Dictionary<string, string>();
            SigDict.Add("Signature", signature);
            //return the encrypted file 
            return new JsonResult(SigDict);
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

        // Requires safe updates to be disabled within MySQl editor preferences
        // DELETE: crypter.dev/api/file/
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            await query.DeleteAllAsync();
            return new OkResult();
        }
    }
}