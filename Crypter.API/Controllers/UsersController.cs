using Crypter.API.Logic;
using Crypter.API.Models;
using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests.Registered;
using Crypter.Contracts.Responses.Registered;
using Crypter.Contracts.Responses.Search;
using Crypter.CryptoLib.Enums;
using Crypter.DataAccess.FileSystem;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IKeyService _keyService;
        private readonly IBaseItemService<MessageItem> _messageService;
        private readonly IBaseItemService<FileItem> _fileService;
        private readonly AppSettings _appSettings;
        private readonly string BaseSaveDirectory;
        private readonly long AllocatedDiskSpace;
        private readonly int MaxUploadSize;
        private const DigestAlgorithm ItemDigestAlgorithm = DigestAlgorithm.SHA256;

        public UsersController(
            IUserService userService,
            IKeyService keyService,
            IBaseItemService<MessageItem> messageService,
            IBaseItemService<FileItem> fileService,
            IOptions<AppSettings> appSettings,
            IConfiguration configuration
            )
        {
            _userService = userService;
            _keyService = keyService;
            _messageService = messageService;
            _fileService = fileService;
            _appSettings = appSettings.Value;
            BaseSaveDirectory = configuration["EncryptedFileStore:Location"];
            AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(1024, 3);
            MaxUploadSize = int.Parse(configuration["MaxUploadSizeMB"]) * (int)Math.Pow(1024, 2);
        }

        // POST: crypter.dev/api/user/register
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterUserRequest body)
        {
            if (!AuthRules.IsValidPassword(body.Password))
            {
                return new BadRequestObjectResult(
                    new UserRegisterResponse(ResponseCode.PasswordRequirementsNotMet));
            }

            var insertResult = await _userService.InsertAsync(body.Username, body.Password, body.Email);
            var responseObject = new UserRegisterResponse(insertResult);

            if (insertResult == InsertUserResult.Success)
            {
                return new OkObjectResult(responseObject);
            }
            else
            {
                return new BadRequestObjectResult(responseObject);
            }
        }

        // POST: crypter.dev/api/user/authenticate
        [HttpPost("authenticate")]
        public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticateUserRequest body)
        {
            var user = await _userService.AuthenticateAsync(body.Username, body.Password);
            if (user == null)
            {
                return new BadRequestObjectResult(new UserAuthenticateResponse(ResponseCode.InvalidCredentials));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.TokenSecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var userPersonalKey = await _keyService.GetUserPersonalKeyAsync(user.Id);

            return new OkObjectResult(
                new UserAuthenticateResponse(user.Id.ToString(), tokenString, userPersonalKey?.PrivateKey)
            );
        }

        // GET: crypter.dev/api/user/account-details
        [Authorize]
        [HttpGet("account-details")]
        public async Task<IActionResult> GetAccountDetailsAsync()
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            var user = await _userService.ReadAsync(Guid.Parse(userId));

            if (user == null)
            {
                return new NotFoundObjectResult(
                    new AccountDetailsResponse(ResponseCode.NotFound));
            }

            var response = new AccountDetailsResponse(
                user.UserName,
                user.Email,
                user.IsPublic,
                user.PublicAlias,
                user.AllowAnonymousFiles,
                user.AllowAnonymousMessages,
                user.Created
            );
            return new OkObjectResult(response);
        }

        // GET: crypter.dev/api/user/user-uploads
        [Authorize]
        [HttpGet("user-uploads")]
        public async Task<IActionResult> GetUserUploadsAsync()
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            var sentMessages = await _messageService.FindBySenderAsync(Guid.Parse(userId));
            var sentFiles = await _fileService.FindBySenderAsync(Guid.Parse(userId));

            var allSentItems = sentMessages
                .Select(x => new UserUploadItemDTO(x.Id.ToString(), x.Subject, ResourceType.Message, x.Expiration))
                .Concat(sentFiles
                    .Select(x => new UserUploadItemDTO(x.Id.ToString(), x.FileName, ResourceType.File, x.Expiration)));

            return new OkObjectResult(new UserUploadsResponse(allSentItems));
        }

        // PUT: crypter.dev/api/user/update-credentials
        [Authorize]
        [HttpPut("update-credentials")]
        public async Task<IActionResult> UpdateUserCredentialsAsync([FromBody] UpdateUserCredentialsRequest body)
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            var user = await _userService.ReadAsync(Guid.Parse(userId));

            if (user == null)
            {
                return new NotFoundObjectResult(
                    new UpdateUserCredentialsResponse(ResponseCode.NotFound));
            }

            var updateResult = await _userService.UpdateCredentialsAsync(Guid.Parse(userId), user.UserName, body.Password);
            var responseObject = new UpdateUserCredentialsResponse(updateResult);

            if (updateResult == UpdateUserCredentialsResult.Success)
            {
                return new OkObjectResult(responseObject);
            }
            else
            {
                return new BadRequestObjectResult(responseObject);
            }
        }

        // PUT: crypter.dev/api/user/update-preferences
        [Authorize]
        [HttpPut("update-preferences")]
        public async Task<IActionResult> UpdateUserPreferencesAsync([FromBody] RegisteredUserPublicSettingsRequest body)
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            var user = await _userService.ReadAsync(Guid.Parse(userId));

            if (user == null)
            {
                return new NotFoundObjectResult(
                    new RegisteredUserPublicSettingsResponse(UpdateUserPreferencesResult.UserNotFound));
            }

            var updateResult = await _userService.UpdatePreferencesAsync(Guid.Parse(userId), body.IsPublic, body.AllowAnonymousFiles, body.AllowAnonymousMessages);
            var responseObject = new RegisteredUserPublicSettingsResponse(updateResult);

            if (updateResult == UpdateUserPreferencesResult.Success)
            {
                return new OkObjectResult(responseObject);
            }
            else
            {
                return new BadRequestObjectResult(responseObject);
            }
        }

        //POST: crypter.dev/api/user/upload
        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadNewItem([FromBody] RegisteredUserUploadRequest body)
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            if (!UploadRules.IsValidUploadRequest(body.CipherText, body.ServerEncryptionKey))
            {
                return new BadRequestObjectResult(
                    new RegisteredUserUploadResponse(ResponseCode.InvalidRequest));
            }

            if (!await UploadRules.AllocatedSpaceRemaining(_messageService, _fileService, AllocatedDiskSpace, MaxUploadSize))
            {
                return new BadRequestObjectResult(
                    new RegisteredUserUploadResponse(ResponseCode.DiskFull));
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

            var saveResult = filepaths.SaveToFileSystem(newGuid, cipherTextBytesServerEncrypted, body.Signature, isFile);
            if (!saveResult)
            {
                return new BadRequestObjectResult(
                    new RegisteredUserUploadResponse(ResponseCode.Unknown));
            }
            var size = filepaths.FileSizeBytes(filepaths.ActualPathString);

            switch (body.Type)
            {
                case ResourceType.Message:

                    var messageItem = new MessageItem(
                        newGuid,
                        Guid.Parse(userId),
                        body.Name,
                        size,
                        filepaths.ActualPathString,
                        filepaths.SigPathString,
                        iv,
                        serverDigest,
                        now,
                        expiration);

                    await _messageService.InsertAsync(messageItem);
                    break;
                case ResourceType.File:
                    var fileItem = new FileItem(
                        newGuid,
                        Guid.Parse(userId),
                        body.Name,
                        body.ContentType,
                        size,
                        filepaths.ActualPathString,
                        filepaths.SigPathString,
                        iv,
                        serverDigest,
                        now,
                        expiration);

                    await _fileService.InsertAsync(fileItem);
                    expiration = fileItem.Expiration;
                    break;
                default:
                    return new OkObjectResult(
                        new RegisteredUserUploadResponse(ResponseCode.InvalidRequest));
            }

            return new JsonResult(
                new RegisteredUserUploadResponse(newGuid, expiration));
        }

        // POST: crypter.dev/api/user/update-personal-keys
        [Authorize]
        [HttpPost("update-personal-keys")]
        public async Task<IActionResult> UpdatePersonalKeys([FromBody] UpdateUserKeysRequest body)
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            var insertResult = await _keyService.InsertUserPersonalKeyAsync(Guid.Parse(userId), body.EncryptedPrivateKey, body.PublicKey);
            if (insertResult)
            {
                return new OkObjectResult(
                    new UpdateUserKeysResponse(ResponseCode.Success));
            }
            else
            {
                return new BadRequestObjectResult(
                    new UpdateUserKeysResponse(ResponseCode.InvalidRequest));
            }
        }

        [Authorize]
        [HttpGet("search/username")]
        public async Task<IActionResult> SearchByUsername([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
        {
            var allMatchingUsers = await _userService.SearchByUsernameAsync(value, index, count);
            var filteredMatches = allMatchingUsers
                .Select(x => new UserSearchResultDTO(x.Id.ToString(), x.UserName, x.PublicAlias))
                .ToList();

            return new OkObjectResult(
                new UserSearchResponse(filteredMatches));
        }

        [Authorize]
        [HttpGet("search/public-alias")]
        public async Task<IActionResult> SearchByPublicAlias([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
        {
            var allMatchingUsers = await _userService.SearchByPublicAliasAsync(value, index, count);
            var filteredMatches = allMatchingUsers
                .Select(x => new UserSearchResultDTO(x.Id.ToString(), x.UserName, x.PublicAlias))
                .ToList();

            return new OkObjectResult(
                new UserSearchResponse(filteredMatches));
        }
    }
}
