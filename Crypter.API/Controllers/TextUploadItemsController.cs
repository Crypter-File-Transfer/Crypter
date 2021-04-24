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

        // Probably not a use case for this GET
        // GET: crypter.dev/api/message
        [HttpGet]
        public async Task<IActionResult> GetTextUploadItems()
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.LatestItemsAsync();
            return new OkObjectResult(result);
        }

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
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //obtain file path for actual encrypted message
            Console.WriteLine(result.CipherTextPath);
            //return the encrypted message 
            return new OkObjectResult(result.CipherTextPath);
        }

        // GET: crypter.dev/api/message/signature/{guid}
        [HttpGet("signature/{id}")]
        public async Task<IActionResult> GetTextUploadSig(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //obtain file path for signature of encrypted message
            Console.WriteLine(result.SignaturePath);
            //read and return signature
            string signature = System.IO.File.ReadAllText(result.SignaturePath);
            Console.WriteLine(signature);
            //Send signature in response-
            Dictionary<string, string> SigDict = new Dictionary<string, string>();
            SigDict.Add("Signature", signature);
            //return the encrypted file 
            return new JsonResult(SigDict); 
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
            //result.ID = body.ID;
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

        // Requires safe updates to be disabled within MySQl editor preferences
        // DELETE: crypter.dev/api/message/
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            await query.DeleteAllAsync();
            return new OkResult();
        }
    }
}