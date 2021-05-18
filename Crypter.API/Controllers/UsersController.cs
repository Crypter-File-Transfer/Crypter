using System;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration; 
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Crypter.DataAccess; 
using Crypter.DataAccess.Models;
using Crypter.DataAccess.Helpers;
using Crypter.DataAccess.Queries;
using Crypter.Contracts; 
using Crypter.Contracts.Requests.Registered;
using Crypter.Contracts.Responses.Registered;
using Crypter.API.Services;
using Crypter.API.Helpers;
using Crypter.Contracts.Enum;
using System.Linq;
using Crypter.API.Logic;
using Newtonsoft.Json;
using System.Threading.Tasks;


namespace Crypter.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IKeyService _keyService;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;
        private readonly CrypterDB Database;
        private readonly string BaseSaveDirectory;
        private readonly long AllocatedDiskSpace;
        private readonly int MaxUploadSize;

        public UsersController(
            IUserService userService,
            IKeyService keyService,
            IMapper mapper,
            IOptions<AppSettings> appSettings,
            CrypterDB db,
            IConfiguration configuration
            )
        {
            _userService = userService;
            _keyService = keyService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            Database = db;
            BaseSaveDirectory = configuration["EncryptedFileStore:Location"];
            AllocatedDiskSpace = long.Parse(configuration["EncryptedFileStore:AllocatedGB"]) * (long)Math.Pow(1024, 3);
            MaxUploadSize = int.Parse(configuration["MaxUploadSizeMB"]) * (int)Math.Pow(1024, 2);
        }

        // POST: crypter.dev/api/user/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterUserRequest body)
        {
            if (!AuthRules.IsValidPassword(body.Password))
            {
                return new BadRequestObjectResult(
                    new UserRegisterResponse(ResponseCode.PasswordRequirementsNotMet));
            }

            var user = _mapper.Map<User>(body);
            try
            {
                User newUser = _userService.Create(user, body.Password);
                return new OkObjectResult(
                    new UserRegisterResponse(ResponseCode.Success));
            }
            catch (AppException ex)
            {
                Console.WriteLine(ex.Message);
                return new BadRequestObjectResult(
                    new UserRegisterResponse(ResponseCode.InvalidRequest));
            }
        }

        // POST: crypter.dev/api/user/authenticate
        [HttpPost("authenticate")]
        public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticateUserRequest body)
        {
            var user = _userService.Authenticate(body.Username, body.Password);

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
                    new Claim(ClaimTypes.Name, user.UserID)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var userPersonalKey = await _keyService.GetUserPersonalKeyAsync(user.UserID);

            return new OkObjectResult(
                new UserAuthenticateResponse(user.UserID, tokenString, userPersonalKey?.PrivateKey)
            );
        }

        // GET: crypter.dev/api/user/account-details
        [Authorize]
        [HttpGet("account-details")]
        public IActionResult GetAccountDetailsAsync()
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            try
            {
                var user = _userService.GetById(userId);
                var response = new AccountDetailsResponse(
                    user.UserName,
                    user.Email,
                    user.IsPublic,
                    user.PublicAlias,
                    user.AllowAnonFiles,
                    user.AllowAnonMessages,
                    user.UserCreated
                );

                return new OkObjectResult(response);
            }
            catch (Exception)
            {
                return new NotFoundObjectResult(
                    new AccountDetailsResponse(ResponseCode.NotFound));
            }
        }

        // GET: crypter.dev/api/user/user-uploads
        [HttpGet("user-uploads")]
        public IActionResult GetUserUploads()
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            try
            {
                var uploadsList = _userService.GetUploadsById(userId);
                var response = JsonConvert.SerializeObject(uploadsList); 
                return new OkObjectResult(new UserUploadsResponse(response));
               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); 
                return new NotFoundObjectResult(
                    new UserUploadsResponse(ResponseCode.InvalidRequest));
            }
        }

        // PUT: crypter.dev/api/user/update-credentials
        [Authorize]
        [HttpPut("update-credentials")]
        public IActionResult UpdateUserCredentials([FromBody] UpdateUserCredentialsRequest body)
        {
            var user = _mapper.Map<User>(body);
            user.UserID = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            try
            {
                // update user 
                _userService.Update(user, body.Password);
                return new OkObjectResult(
                    new UpdateUserCredentialsResponse(ResponseCode.Success));
            }
            catch (AppException ex)
            {
                Console.WriteLine(ex.Message);
                return new BadRequestObjectResult(
                    new UpdateUserCredentialsResponse(ResponseCode.InvalidRequest));
            }
        }

        // PUT: crypter.dev/api/user/update-preferences
        [Authorize]
        [HttpPut("update-preferences")]
        public IActionResult UpdateUserPreferences([FromBody] RegisteredUserPublicSettingsRequest body)
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            var user = new User(userId, body.PublicAlias, body.IsPublic, body.AllowAnonymousMessages, body.AllowAnonymousFiles);

            try
            {
                _userService.UpdatePublic(user);
                return new OkObjectResult(
                    new RegisteredUserPublicSettingsResponse(user.PublicAlias, user.IsPublic, user.AllowAnonMessages, user.AllowAnonFiles));
            }
            catch (AppException ex)
            {
                Console.WriteLine(ex.Message);
                return new BadRequestObjectResult(
                    new RegisteredUserPublicSettingsResponse(ResponseCode.InvalidRequest));
            }
        }

        //POST: crypter.dev/api/user/upload
        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadNewItem([FromBody] RegisteredUserUploadRequest body)
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
            Console.WriteLine(userId); 
            if (!UploadRules.IsValidRegisteredUserUploadRequest(body))
            {
                return new OkObjectResult(
                    new RegisteredUserUploadResponse(ResponseCode.InvalidRequest));
            }

            Database.Connection.Open();

            if (!await UploadRules.AllocatedSpaceRemaining(Database, AllocatedDiskSpace, MaxUploadSize))
            {
                return new OkObjectResult(
                    new RegisteredUserUploadResponse(ResponseCode.DiskFull));
            }

            Guid newGuid;
            DateTime expiration;
            switch (body.Type)
            {
                case ResourceType.Message:
                    var newText = new TextUploadItem
                    {
                        UserID = userId, 
                        FileName = body.Name,
                        CipherText = body.CipherText,
                        Signature = body.Signature,
                        ServerEncryptionKey = body.ServerEncryptionKey
                    };
                    await newText.InsertAsync(Database, BaseSaveDirectory);
                    newGuid = Guid.Parse(newText.ID);
                    expiration = newText.ExpirationDate;
                    break;
                case ResourceType.File:
                    var newFile = new FileUploadItem
                    {
                        UserID = userId,
                        FileName = body.Name,
                        ContentType = body.ContentType,
                        CipherText = body.CipherText,
                        Signature = body.Signature,
                        ServerEncryptionKey = body.ServerEncryptionKey
                    };
                    await newFile.InsertAsync(Database, BaseSaveDirectory);
                    newGuid = Guid.Parse(newFile.ID);
                    expiration = newFile.ExpirationDate;
                    break;
                default:
                    return new OkObjectResult(
                        new RegisteredUserUploadResponse(ResponseCode.InvalidRequest));
            }

            var responseBody = new RegisteredUserUploadResponse(newGuid, expiration);
            return new JsonResult(responseBody);
        }

        // POST: crypter.dev/api/user/update-personal-keys
        [Authorize]
        [HttpPost("update-personal-keys")]
        public async Task<IActionResult> UpdatePersonalKeys([FromBody] UpdateUserKeysRequest body)
        {
            var userId = User.Claims.First(x => x.Type == ClaimTypes.Name).Value;

            try
            {
                await _keyService.InsertUserPersonalKeyAsync(userId, body.EncryptedPrivateKey, body.PublicKey);
                return new OkObjectResult(
                    new UpdateUserKeysResponse(ResponseCode.Success));
            }
            catch (AppException ex)
            {
                Console.WriteLine(ex.Message);
                return new BadRequestObjectResult(
                    new UpdateUserKeysResponse(ResponseCode.InvalidRequest));
            }
        }
    }
}
