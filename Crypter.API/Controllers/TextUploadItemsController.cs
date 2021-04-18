using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CrypterAPI.Models;
using System;

namespace CrypterAPI.Controllers
{
    [Route("message")]
    //[ApiController]
    public class TextUploadItemsController : ControllerBase
    {
        public CrypterDB Db { get; }

        public TextUploadItemsController(CrypterDB db)
        {
            Db = db;
        }

        // POST: crypter.dev/message
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostTextUploadItem([FromBody] TextUploadItem body)
        {
            await Db.Connection.OpenAsync();
            body.Db = Db;
            await body.InsertAsync();
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

        // GET: crypter.dev/message{guid}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTextUploadItem(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new TextUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            return new OkObjectResult(result);
        }

        // PUT: crypter.dev/message/{guid}
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
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
            result.EncryptedMessagePath = body.EncryptedMessagePath;
            await result.UpdateAsync();
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
            await result.DeleteAsync();
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