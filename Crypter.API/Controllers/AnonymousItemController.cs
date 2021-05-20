﻿using Crypter.API.Logic;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests.Anonymous;
using Crypter.Contracts.Responses.Anonymous;
using Crypter.CryptoLib.Enums;
using Crypter.DataAccess;
using Crypter.DataAccess.Helpers;
using Crypter.DataAccess.Models;
using Crypter.DataAccess.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
    [Route("api/anonymous")]
    [Produces("application/json")]
    //[ApiController]
    public class AnonymousItemController : ControllerBase
    {
        private readonly CrypterDB Database;
        private readonly string BaseSaveDirectory;
        private readonly long AllocatedDiskSpace;
        private readonly int MaxUploadSize;
        private const DigestAlgorithm ItemDigestAlgorithm = DigestAlgorithm.SHA256;

        public AnonymousItemController(CrypterDB db, IConfiguration configuration)
        {
            Database = db;
            BaseSaveDirectory = configuration["EncryptedFileStore:Location"];
            AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(1024, 3);
            MaxUploadSize = int.Parse(configuration["MaxUploadSizeMB"]) * (int)Math.Pow(1024, 2);
        }

        // POST: crypter.dev/api/anonymous/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadNewItem([FromBody] AnonymousUploadRequest body)
        {
            if (!UploadRules.IsValidUploadRequest(body))
            {
                return new OkObjectResult(
                    new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            Database.Connection.Open();

            if (!await UploadRules.AllocatedSpaceRemaining(Database, AllocatedDiskSpace, MaxUploadSize))
            {
                return new OkObjectResult(
                    new AnonymousUploadResponse(ResponseCode.DiskFull));
            }

            // Digest the ciphertext BEFORE applying server-side encryption
            var ciphertextBytes = Convert.FromBase64String(body.CipherText);
            var serverDigest = CryptoLib.Common.GetDigest(ciphertextBytes, ItemDigestAlgorithm);
            var encodedServerDigest = Convert.ToBase64String(serverDigest);

            Guid newGuid;
            DateTime expiration;
            switch (body.Type)
            {
                case ResourceType.Message:
                    var newText = new TextUploadItem
                    {
                        UserID = Guid.Empty.ToString(), 
                        FileName = body.Name,
                        CipherText = body.CipherText,
                        Signature = body.Signature,
                        ServerEncryptionKey = body.ServerEncryptionKey,
                        ServerDigest = encodedServerDigest
                    };
                    await newText.InsertAsync(Database, BaseSaveDirectory);
                    newGuid = Guid.Parse(newText.ID);
                    expiration = newText.ExpirationDate;
                    break;
                case ResourceType.File:
                    var newFile = new FileUploadItem
                    {
                        UserID = Guid.Empty.ToString(),
                        FileName = body.Name,
                        ContentType = body.ContentType,
                        CipherText = body.CipherText,
                        Signature = body.Signature,
                        ServerEncryptionKey = body.ServerEncryptionKey,
                        ServerDigest = encodedServerDigest
                    };
                    await newFile.InsertAsync(Database, BaseSaveDirectory);
                    newGuid = Guid.Parse(newFile.ID);
                    expiration = newFile.ExpirationDate;
                    break;
                default:
                    return new OkObjectResult(
                        new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            var responseBody = new AnonymousUploadResponse(newGuid, expiration);
            return new JsonResult(responseBody);
        }

        // POST: crypter.dev/api/anonymous/get-preview
        [HttpPost("get-preview")]
        public async Task<IActionResult> GetItemPreview([FromBody] AnonymousPreviewRequest body)
        {
            Database.Connection.Open();

            AnonymousPreviewResponse response;
            switch (body.Type)
            {
                case ResourceType.Message:
                    var textQuery = new TextUploadItemQuery(Database);
                    var textResult = await textQuery.FindOneAsync(body.Id.ToString());

                    if (textResult is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousPreviewResponse(ResponseCode.NotFound));
                    }
                    response = new AnonymousPreviewResponse(textResult.FileName, "text", textResult.Size, textResult.Created, textResult.ExpirationDate);

                    break;
                case ResourceType.File:
                    var fileQuery = new FileUploadItemQuery(Database);
                    var fileResult = await fileQuery.FindOneAsync(body.Id.ToString());

                    if (fileResult is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousPreviewResponse(ResponseCode.NotFound));
                    }
                    response = new AnonymousPreviewResponse(fileResult.FileName, fileResult.ContentType, fileResult.Size, fileResult.Created, fileResult.ExpirationDate);

                    break;
                default:
                    return new OkObjectResult(
                        new AnonymousUploadResponse(ResponseCode.InvalidRequest));

            }

            return new OkObjectResult(response);
        }

        // POST: crypter.dev/api/anonymous/get-item
        [HttpPost("get-item")]
        public async Task<IActionResult> GetItem([FromBody] AnonymousDownloadRequest body)
        {
            Database.Connection.Open();

            byte[] serverDecryptionKey = Convert.FromBase64String(body.ServerDecryptionKey);
            string cipherTextPath;
            byte[] iv;
            byte[] storedServerDigest;
            Func<CrypterDB, Task> deleteRecord;
            switch (body.Type)
            {
                case ResourceType.Message:
                    var textQuery = new TextUploadItemQuery(Database);
                    var textResult = await textQuery.FindOneAsync(body.Id.ToString());

                    if (textResult is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousDownloadResponse(ResponseCode.NotFound));
                    }

                    cipherTextPath = textResult.CipherTextPath;
                    iv = Convert.FromBase64String(textResult.InitializationVector);
                    storedServerDigest = Convert.FromBase64String(textResult.ServerDigest);
                    deleteRecord = textResult.DeleteAsync;

                    break;
                case ResourceType.File:
                    var fileQuery = new FileUploadItemQuery(Database);
                    var fileResult = await fileQuery.FindOneAsync(body.Id.ToString());

                    if (fileResult is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousDownloadResponse(ResponseCode.NotFound));
                    }

                    cipherTextPath = fileResult.CipherTextPath;
                    iv = Convert.FromBase64String(fileResult.InitializationVector);
                    storedServerDigest = Convert.FromBase64String(fileResult.ServerDigest);
                    deleteRecord = fileResult.DeleteAsync;

                    break;
                default:
                    return new OkObjectResult(
                        new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            // Remove server-side encryption
            byte[] cipherTextServer = System.IO.File.ReadAllBytes(cipherTextPath);
            var symParams = CryptoLib.Common.MakeSymmetricCryptoParams(serverDecryptionKey, iv);
            byte[] cipherTextClient = CryptoLib.Common.UndoSymmetricEncryption(cipherTextServer, symParams);

            // Compare digests AFTER removing server-side encryption
            var digestsMatch = CryptoLib.Common.VerifyPlaintextAgainstKnownDigest(cipherTextClient, storedServerDigest, ItemDigestAlgorithm);
            if (!digestsMatch)
            {
                return new BadRequestObjectResult(
                    new AnonymousDownloadResponse(ResponseCode.DigestsDoNotMatch));
            }

            // Delete the item from the server
            await deleteRecord(Database);
            FileCleanup DownloadDir = new FileCleanup(body.Id.ToString(), BaseSaveDirectory);
            if (body.Type == ResourceType.File)
                DownloadDir.CleanDirectory(true);
            else
            {
                DownloadDir.CleanDirectory(false);
            }
            return new OkObjectResult(
                new AnonymousDownloadResponse(Convert.ToBase64String(cipherTextClient)));
        }

        // POST: crypter.dev/api/anonymous/get-signature
        [HttpPost("get-signature")]
        public async Task<IActionResult> GetItemSignature([FromBody] AnonymousSignatureRequest request)
        {
            Database.Connection.Open();

            string signaturePath;
            switch (request.Type)
            {
                case ResourceType.Message:
                    var textQuery = new TextUploadItemQuery(Database);
                    var textResult = await textQuery.FindOneAsync(request.Id.ToString());

                    if (textResult is null)
                    {
                        return new OkObjectResult(
                            new NotFoundObjectResult(ResponseCode.NotFound));
                    }

                    signaturePath = textResult.SignaturePath;
                    break;
                case ResourceType.File:
                    var fileQuery = new FileUploadItemQuery(Database);
                    var fileResult = await fileQuery.FindOneAsync(request.Id.ToString());

                    if (fileResult is null)
                    {
                        return new OkObjectResult(
                            new NotFoundObjectResult(ResponseCode.NotFound));
                    }

                    signaturePath = fileResult.SignaturePath;
                    break;
                default:
                    return new OkObjectResult(
                        new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            string signature = System.IO.File.ReadAllText(signaturePath);
            return new OkObjectResult(
                new AnonymousSignatureResponse(signature));
        }
    }
}