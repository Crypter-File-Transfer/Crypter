using System;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Crypter.DataAccess.Models;
using Crypter.Contracts.Requests.Registered;
using Crypter.Contracts.Responses.Registered;
using Crypter.API.Services;
using Crypter.API.Helpers;
using Crypter.Contracts.Enum;
using System.Linq;
using Crypter.API.Logic;
using Newtonsoft.Json;

namespace Crypter.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UsersController(
            IUserService userService,
            IMapper mapper,
            IOptions<AppSettings> appSettings
            )
        {
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
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
        public IActionResult Authenticate([FromBody] AuthenticateUserRequest body)
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

            return new OkObjectResult(
                new UserAuthenticateResponse(tokenString)
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
            catch (Exception)
            {
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
                    new RegisteredUserPublicSettingsResponse(user.PublicAlias, user.IsPublic, user.AllowAnonMessages, user.AllowAnonFiles)
                );
            }
            catch (AppException)
            {
                return new BadRequestObjectResult(
                    new RegisteredUserPublicSettingsResponse(ResponseCode.InvalidRequest)
                );
            }
        }
    }
}
