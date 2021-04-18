using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrypterAPI.Models;
using System;
using Crypter.API.Controllers;

namespace CrypterAPI.Controllers
{
    [Route("file")]
    //[ApiController]
    public class FileUploadItemsController : ControllerBase
    {
        public CrypterDB Db { get; }

        public FileUploadItemsController(CrypterDB db)
        {
            Db = db;
        }

        // POST: crypter.dev/file
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostFileUploadItem([FromBody] FileUploadItem body)
        {
            await Db.Connection.OpenAsync();
            body.Db = Db;
            await body.InsertAsync();
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

        // GET: crypter.dev/file/{guid}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFileUploadItem(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            Console.WriteLine(result.EncryptedFileContentPath); 
            return new OkObjectResult(result.EncryptedFileContentPath);
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
            result.Signature = body.Signature;
            result.Created = body.Created;
            result.ExpirationDate = body.ExpirationDate;
            result.EncryptedFileContentPath = body.EncryptedFileContentPath;
            await result.UpdateAsync();
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
            await result.DeleteAsync();
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