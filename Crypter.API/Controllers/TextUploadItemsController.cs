using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrypterAPI.Models;
using Microsoft.Extensions.Configuration;
using System;

namespace CrypterAPI.Controllers
{
    [Route("message")]
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

        // POST: crypter.dev/message
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostTextUploadItem([FromBody] TextUploadItem body)
        {
            await Db.Connection.OpenAsync();
            await body.InsertAsync(Db, BaseSaveDirectory);
            return new OkObjectResult(body.ID);
        }

        // GET: crypter.dev/message
        [HttpGet]
        public async Task<IActionResult> GetTextUploadItems()
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.LatestItemsAsync();
            return new OkObjectResult(result);
        }

        // GET: crypter.dev/message/actual/{guid}
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

        // GET: crypter.dev/message/signature/{guid}
        [HttpGet("signature/{id}")]
        public async Task<IActionResult> GetTextUploadSig(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            //obtain file path for signature of encrypted message
            Console.WriteLine(result.Signature);
            //TODO: read and return signature
            //return the encrypted file 
            return new OkObjectResult(result.Signature);
        }

        // PUT: crypter.dev/message/signature/{guid}
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
            result.UntrustedName = body.UntrustedName;
            result.Size = body.Size;
            result.Signature = body.Signature;
            result.Created = body.Created;
            result.ExpirationDate = body.ExpirationDate;
            result.CipherTextPath = body.CipherTextPath;
            await result.UpdateAsync(Db);
            return new OkObjectResult(result);

        }

        // DELETE: crypter.dev/message/{guid}
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
        // DELETE: crypter.dev/message/
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