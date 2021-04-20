using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrypterAPI.Models;
using System;
using Microsoft.Extensions.Configuration;

namespace CrypterAPI.Controllers
{
    [Route("file")]
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

        // POST: crypter.dev/file
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostFileUploadItem([FromBody] FileUploadItem body)
        {
            await Db.Connection.OpenAsync();
            await body.InsertAsync(Db, BaseSaveDirectory);
            return new OkObjectResult(body.ID);
        }

        // GET: crypter.dev/file
        [HttpGet]
        public async Task<IActionResult> GetFileUploadItems()
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.LatestItemsAsync();
            return new OkObjectResult(result);
        }

        // GET: crypter.dev/file/actual/{guid}
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
            return new OkObjectResult(result.CipherTextPath);
        }

        // GET: crypter.dev/file/signature/{guid}
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
            //return the sig
            return new OkObjectResult(result.SignaturePath);
        }

        // PUT: crypter.dev/file/{guid}
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
            result.UntrustedName = body.UntrustedName;
            result.Size = body.Size;
            result.SignaturePath = body.SignaturePath;
            result.Created = body.Created;
            result.ExpirationDate = body.ExpirationDate;
            result.CipherTextPath = body.CipherTextPath;
            await result.UpdateAsync(Db);
            return new OkObjectResult(result);

        }

        // DELETE: crypter.dev/file/{guid}
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
        // DELETE: crypter.dev/file/
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