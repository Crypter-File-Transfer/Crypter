using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CrypterAPI.Models;
using System; 

namespace CrypterAPI.Controllers
{
    [Route("api/[controller]")]
    //[ApiController]
    public class FileUploadItemsController : ControllerBase
    {
        public CrypterDB Db { get; }

        public FileUploadItemsController(CrypterDB db)
        {
            Db = db;
        }

        // POST: api/FileUploadItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostFileUploadItem([FromBody] FileUploadItem body)
        {
            await Db.Connection.OpenAsync();
            body.Db = Db;
            await body.InsertAsync();
            //Create folder for uploaded file
            string folderName = @"../../../CrypterFiles";
            string pathString = System.IO.Path.Combine(folderName, $"{body.ID}");
            System.IO.Directory.CreateDirectory(pathString);
            string fileName = $"{body.UntrustedName}";
            string signatureName = $"{body.Signature}";
            string actualPathString = System.IO.Path.Combine(pathString, fileName);
            string sigPathString = System.IO.Path.Combine(pathString, signatureName);
            //Confirm paths 
            Console.WriteLine("Newly created file path: {0}", actualPathString);
            Console.WriteLine("New created signature path: {0}", sigPathString);
            //return GUID
            Console.Write($"{body.ID}\n"); 
            return new OkObjectResult(body.ID);
        }

        // GET: api/FileUploadItems
        [HttpGet]
        public async Task<IActionResult> GetFileUploadItems()
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.LatestItemsAsync();
            return new OkObjectResult(result);
        }

        // GET: api/FileUploadItems/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFileUploadItem(string id)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            return new OkObjectResult(result);
        }

        // PUT: api/FileUploadItems/5
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

        // DELETE: api/FileUploadItems/5
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
        // DELETE api/FileUploadItems
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