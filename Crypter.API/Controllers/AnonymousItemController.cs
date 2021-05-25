using Crypter.API.Logic;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests.Anonymous;
using Crypter.Contracts.Responses.Anonymous;
using Crypter.CryptoLib.Enums;
using Crypter.DataAccess.FileSystem;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
    [Route("api/anonymous")]
    public class AnonymousItemController : ControllerBase
    {
        private readonly string BaseSaveDirectory;
        private readonly long AllocatedDiskSpace;
        private readonly int MaxUploadSize;
        private const DigestAlgorithm ItemDigestAlgorithm = DigestAlgorithm.SHA256;
        private readonly IBaseItemService<MessageItem> _messageService;
        private readonly IBaseItemService<FileItem> _fileService;

        public AnonymousItemController(IConfiguration configuration,
            IBaseItemService<MessageItem> messageService,
            IBaseItemService<FileItem> fileService
            )
        {
            BaseSaveDirectory = configuration["EncryptedFileStore:Location"];
            AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(1024, 3);
            MaxUploadSize = int.Parse(configuration["MaxUploadSizeMB"]) * (int)Math.Pow(1024, 2);
            _messageService = messageService;
            _fileService = fileService;
        }

        // POST: crypter.dev/api/anonymous/upload
        [HttpPost("upload")]
        public async Task<IActionResult> UploadNewItem([FromBody] AnonymousUploadRequest body)
        {
            if (!UploadRules.IsValidUploadRequest(body.CipherText, body.ServerEncryptionKey))
            {
                return new BadRequestObjectResult(
                    new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            if (!await UploadRules.AllocatedSpaceRemaining(_messageService, _fileService, AllocatedDiskSpace, MaxUploadSize))
            {
                return new BadRequestObjectResult(
                    new AnonymousUploadResponse(ResponseCode.DiskFull));
            }

            // Digest the ciphertext BEFORE applying server-side encryption
            var ciphertextBytesClientEncrypted = Convert.FromBase64String(body.CipherText);
            var serverDigest = CryptoLib.Common.GetDigest(ciphertextBytesClientEncrypted, ItemDigestAlgorithm);

            // Apply server-side encryption
            byte[] hashedSymmetricEncryptionKey = Convert.FromBase64String(body.ServerEncryptionKey);
            byte[] iv = CryptoLib.BouncyCastle.SymmetricMethods.GenerateIV();
            var symmetricParams = CryptoLib.Common.MakeSymmetricCryptoParams(hashedSymmetricEncryptionKey, iv);
            byte[] cipherTextBytesServerEncrypted = CryptoLib.Common.DoSymmetricEncryption(ciphertextBytesClientEncrypted, symmetricParams);

            Guid newGuid = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var expiration = now.AddHours(24);
            var filepaths = new CreateFilePaths(BaseSaveDirectory);
            bool isFile = body.Type == ResourceType.File;

            var saveResult = filepaths.SaveToFileSystem(newGuid, cipherTextBytesServerEncrypted, isFile);
            if (!saveResult)
            {
                return new BadRequestObjectResult(
                    new AnonymousUploadResponse(ResponseCode.Unknown));
            }
            var size = filepaths.FileSizeBytes(filepaths.ActualPathString);

            switch (body.Type)
            {
                case ResourceType.Message:
                    var messageItem = new MessageItem(
                        newGuid,
                        Guid.Empty,
                        body.Name,
                        size,
                        filepaths.ActualPathString,
                        body.Signature,
                        body.EncryptedSymmetricInfo,
                        body.PublicKey,
                        iv,
                        serverDigest,
                        now,
                        expiration);

                    await _messageService.InsertAsync(messageItem);
                    break;
                case ResourceType.File:
                    var fileItem = new FileItem(
                        newGuid,
                        Guid.Empty,
                        body.Name,
                        body.ContentType,
                        size,
                        filepaths.ActualPathString,
                        body.Signature,
                        body.EncryptedSymmetricInfo,
                        body.PublicKey,
                        iv,
                        serverDigest,
                        now,
                        expiration);

                    await _fileService.InsertAsync(fileItem);
                    expiration = fileItem.Expiration;
                    break;
                default:
                    return new OkObjectResult(
                        new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            return new OkObjectResult(
                new AnonymousUploadResponse(newGuid, expiration));
        }

        // POST: crypter.dev/api/anonymous/get-preview
        [HttpPost("get-preview")]
        public async Task<IActionResult> GetItemPreview([FromBody] AnonymousPreviewRequest body)
        {
            AnonymousPreviewResponse response;
            switch (body.Type)
            {
                case ResourceType.Message:
                    var foundMessage = await _messageService.ReadAsync(body.Id);

                    if (foundMessage is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousPreviewResponse(ResponseCode.NotFound));
                    }
                    response = new AnonymousPreviewResponse(foundMessage.Subject, "text", foundMessage.Size, foundMessage.Created, foundMessage.Expiration);

                    break;
                case ResourceType.File:
                    var foundFile = await _fileService.ReadAsync(body.Id);

                    if (foundFile is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousPreviewResponse(ResponseCode.NotFound));
                    }
                    response = new AnonymousPreviewResponse(foundFile.FileName, foundFile.ContentType, foundFile.Size, foundFile.Created, foundFile.Expiration);

                    break;
                default:
                    return new BadRequestObjectResult(
                        new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            return new OkObjectResult(response);
        }

        // POST: crypter.dev/api/anonymous/get-item
        [HttpPost("get-item")]
        public async Task<IActionResult> GetItem([FromBody] AnonymousDownloadRequest body)
        {
            byte[] serverDecryptionKey = Convert.FromBase64String(body.ServerDecryptionKey);
            string cipherTextPath;
            byte[] iv;
            byte[] storedServerDigest;
            switch (body.Type)
            {
                case ResourceType.Message:
                    var foundMessage = await _messageService.ReadAsync(body.Id);

                    if (foundMessage is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousDownloadResponse(ResponseCode.NotFound));
                    }

                    cipherTextPath = foundMessage.CipherTextPath;
                    iv = foundMessage.ServerIV;
                    storedServerDigest = foundMessage.ServerDigest;

                    break;
                case ResourceType.File:
                    var foundFile = await _fileService.ReadAsync(body.Id);

                    if (foundFile is null)
                    {
                        return new NotFoundObjectResult(
                            new AnonymousDownloadResponse(ResponseCode.NotFound));
                    }

                    cipherTextPath = foundFile.CipherTextPath;
                    iv = foundFile.ServerIV;
                    storedServerDigest = foundFile.ServerDigest;

                    break;
                default:
                    return new BadRequestObjectResult(
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
            switch (body.Type)
            {
                case ResourceType.Message:
                    await _messageService.DeleteAsync(body.Id);
                    break;
                case ResourceType.File:
                    await _fileService.DeleteAsync(body.Id);
                    break;
                default:
                    break;
            }

            FileCleanup DownloadDir = new FileCleanup(body.Id, BaseSaveDirectory);
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
            string signature;
            string publicKey;
            string symmetricInfo;
            switch (request.Type)
            {
                case ResourceType.Message:
                    var foundMessage = await _messageService.ReadAsync(request.Id);

                    if (foundMessage is null)
                    {
                        return new NotFoundObjectResult(
                            new NotFoundObjectResult(ResponseCode.NotFound));
                    }

                    signature = foundMessage.Signature;
                    publicKey = foundMessage.PublicKey;
                    symmetricInfo = foundMessage.SymmetricInfo;
                    break;
                case ResourceType.File:
                    var foundFile = await _fileService.ReadAsync(request.Id);

                    if (foundFile is null)
                    {
                        return new NotFoundObjectResult(
                            new NotFoundObjectResult(ResponseCode.NotFound));
                    }

                    signature = foundFile.Signature;
                    publicKey = foundFile.PublicKey;
                    symmetricInfo = foundFile.SymmetricInfo;
                    break;
                default:
                    return new OkObjectResult(
                        new AnonymousUploadResponse(ResponseCode.InvalidRequest));
            }

            return new OkObjectResult(
                new AnonymousSignatureResponse(signature, publicKey, symmetricInfo));
        }
    }
}
