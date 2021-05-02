using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.Extensions.Configuration;
using Crypter.Contracts.Requests.Anonymous;
using Crypter.Contracts.Responses.Anonymous;
using Crypter.Contracts.Enum;
using Crypter.CryptoLib;
using Crypter.DataAccess;
using Crypter.DataAccess.Models;
using Crypter.DataAccess.Queries;
using Crypter.DataAccess.Helpers;

namespace CrypterAPI.Controllers
{
    [Route("api/file")]
    [Produces("application/json")]
    //[ApiController]
    public class FileUploadItemsController : ControllerBase
    {
        private readonly CrypterDB Db;
        private readonly string BaseSaveDirectory;
        private readonly long AllocatedDiskSpace;

        public FileUploadItemsController(CrypterDB db, IConfiguration configuration)
        {
            Db = db;
            BaseSaveDirectory = configuration["EncryptedFileStore:Location"];
            AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * 1024 * 1024 * 1024;
        }

        // POST: crypter.dev/api/file
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> PostFileUploadItem([FromBody] AnonymousFileUploadRequest body)
        {
            await Db.Connection.OpenAsync();

            // Check if the disk is full before saving the upload
            var sizeOfFileUploads = new FileUploadItemQuery(Db).GetSumOfSize();
            var sizeOfMessageUploads = new TextUploadItemQuery(Db).GetSumOfSize();
            var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
            int maxUploadSize = 10 * 1024 * 1024;
            if ((totalSizeOfUploads + maxUploadSize) > AllocatedDiskSpace)
            {
                var outOfSpaceResponseBody = new AnonymousUploadResponse(ResponseCode.DiskFull);
                return new OkObjectResult(outOfSpaceResponseBody);
            }

            var newFile = new FileUploadItem();
            newFile.FileName = body.Name;
            newFile.ContentType = body.ContentType;
            newFile.CipherText = body.CipherText;
            newFile.Signature = body.Signature;
            // if server encryption key is empty and is not 256 bits(32bytes), reject the post
            if (!string.IsNullOrEmpty(body.ServerEncryptionKey))
            {
                byte[] keyString = Convert.FromBase64String(body.ServerEncryptionKey);
                int keySize = Buffer.ByteLength(keyString);
                //check size
                if (keySize == 32)
                {
                    //add server encryption key to the upload item 
                    newFile.ServerEncryptionKey = body.ServerEncryptionKey;
                    await newFile.InsertAsync(Db, BaseSaveDirectory);
                    var responseBody = new AnonymousUploadResponse(Guid.Parse(newFile.ID), newFile.ExpirationDate);
                    return new JsonResult(responseBody);
                }
            }
            //reject posts with no encryption key/ invalid length
            var invalidResponseBody = new AnonymousUploadResponse(ResponseCode.InvalidRequest);
            return new BadRequestObjectResult(invalidResponseBody);
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

            var responseBody = new AnonymousFilePreviewResponse(result.FileName, result.ContentType, result.Size, result.Created, result.ExpirationDate);
            return new OkObjectResult(responseBody);
        }

        // POST: crypter.dev/api/file/actual
        [HttpPost("actual")]
        public async Task<IActionResult> GetFileUploadActual([FromBody] AnonymousFileDownloadRequest body)
        {
            await Db.Connection.OpenAsync();
            var query = new FileUploadItemQuery(Db);
            string downloadId = body.Id.ToString();
            FileUploadItem result = await query.FindOneAsync(downloadId);
            if (result is null)
                return new NotFoundResult();
            //Get decryption key from client
            byte[] ServerDecryptionKey = Convert.FromBase64String(body.ServerDecryptionKey);
            //read bytes from path
            byte[] cipherTextAES = System.IO.File.ReadAllBytes(result.CipherTextPath);
            byte[] initializationVector = Convert.FromBase64String(result.InitializationVector);
            // make symmetric params and remove server-side AES encryption
            var symParams = Common.MakeSymmetricCryptoParams(ServerDecryptionKey, initializationVector);
            byte[] cipherText = Common.UndoSymmetricEncryption(cipherTextAES, symParams);
            //init response body and return cipherText to client
            var responseBody = new AnonymousDownloadResponse(Convert.ToBase64String(cipherText));
            //delete item from db
            await result.DeleteAsync(Db);
            //delete directory content for downloaded Id
            FileCleanup DownloadDir = new FileCleanup(downloadId, BaseSaveDirectory);
            DownloadDir.CleanDirectory(true); 
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
    }
}