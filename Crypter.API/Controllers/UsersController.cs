using System;
using System.Collections.Generic;
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

namespace Crypter.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/user")]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UsersController(
            IUserService userService,
            IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] AuthenticateUserRequest body)
        {
            var user = _userService.Authenticate(body.Username, body.Password);

            if (user == null)
                return new OkObjectResult(new AuthenticateUserResponse(ResponseCode.InvalidCredentials));
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserID.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return basic user info and authentication token
            return new OkObjectResult(
                new AuthenticateUserResponse(user.UserID, user.UserName, tokenString)
            );  
        }



        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterUserRequest body)
        {
         
            //validate password
            if (!UploadRules.IsValidPassword(body.Password))
            {
                return new OkObjectResult(
                    new RegisterUserResponse(ResponseCode.PasswordRequirementsNotMet));
            }
            // map model to entity
            var user = _mapper.Map<User>(body);
            try
            {
                // create user
                User newUser = _userService.Create(user, body.Password);
                return new OkObjectResult(
                new RegisterUserResponse(newUser.UserName, newUser.UserCreated)
                );
            }
            catch (AppException)
            {
                // return error message if there was an exception
                return new OkObjectResult(
                    new RegisterUserResponse(ResponseCode.InvalidRequest)
                ); 
            }
        }
    }
}
