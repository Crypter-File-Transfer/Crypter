using Crypter.API.Logic;
using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.DataAccess.Interfaces;
using Crypter.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/user")]
   public class UserController : ControllerBase
   {
      private readonly IUserService UserService;
      private readonly IKeyService KeyService;
      private readonly IBaseItemService<MessageItem> MessageService;
      private readonly IBaseItemService<FileItem> FileService;
      private readonly IBetaKeyService BetaKeyService;
      private readonly byte[] TokenSecretKey;

      public UserController(
          IUserService userService,
          IKeyService keyService,
          IBaseItemService<MessageItem> messageService,
          IBaseItemService<FileItem> fileService,
          IBetaKeyService betaKeyService,
          IConfiguration configuration
          )
      {
         UserService = userService;
         KeyService = keyService;
         MessageService = messageService;
         FileService = fileService;
         BetaKeyService = betaKeyService;
         TokenSecretKey = Encoding.UTF8.GetBytes(configuration["TokenSecretKey"]);
      }

      [HttpPost("register")]
      public async Task<IActionResult> RegisterAsync([FromBody] RegisterUserRequest request)
      {
         var foundBetaKey = await BetaKeyService.ReadAsync(request.BetaKey);
         if (foundBetaKey == null)
         {
            return new BadRequestObjectResult(
                new UserRegisterResponse(InsertUserResult.InvalidBetaKey));
         }

         if (!AuthRules.IsValidPassword(request.Password))
         {
            return new BadRequestObjectResult(
                new UserRegisterResponse(InsertUserResult.PasswordRequirementsNotMet));
         }

         var insertResult = await UserService.InsertAsync(request.Username, request.Password, request.Email);
         var responseObject = new UserRegisterResponse(insertResult);

         if (insertResult == InsertUserResult.Success)
         {
            await BetaKeyService.DeleteAsync(foundBetaKey.Key);
            return new OkObjectResult(responseObject);
         }
         else
         {
            return new BadRequestObjectResult(responseObject);
         }
      }

      [HttpPost("authenticate")]
      public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticateUserRequest request)
      {
         var user = await UserService.AuthenticateAsync(request.Username, request.Password);
         if (user == null)
         {
            return new NotFoundObjectResult(
               new UserAuthenticateResponse(default, null));
         }

         var tokenHandler = new JwtSecurityTokenHandler();
         var tokenDescriptor = new SecurityTokenDescriptor
         {
            Subject = new ClaimsIdentity(new Claim[]
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }),
            Audience = "crypter.dev",
            Issuer = "crypter.dev/api",
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(TokenSecretKey), SecurityAlgorithms.HmacSha256Signature)
         };
         var token = tokenHandler.CreateToken(tokenDescriptor);
         var tokenString = tokenHandler.WriteToken(token);

         var userPersonalKeys = await KeyService.GetUserPersonalKeyAsync(user.Id);

         return new OkObjectResult(
             new UserAuthenticateResponse(user.Id, tokenString, userPersonalKeys?.PrivateKey)
         );
      }

      [Authorize]
      [HttpGet("authenticate/refresh")]
      public IActionResult RefreshAuthenticationAsync()
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

         var tokenHandler = new JwtSecurityTokenHandler();
         var tokenDescriptor = new SecurityTokenDescriptor
         {
            Subject = new ClaimsIdentity(new Claim[]
            {
               new Claim(ClaimTypes.NameIdentifier, userId)
            }),
            Audience = "crypter.dev",
            Issuer = "crypter.dev/api",
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(TokenSecretKey), SecurityAlgorithms.HmacSha256Signature)
         };
         var token = tokenHandler.CreateToken(tokenDescriptor);
         var tokenString = tokenHandler.WriteToken(token);

         return new OkObjectResult(
            new UserAuthenticationRefreshResponse(tokenString));
      }

      [Authorize]
      [HttpGet("settings")]
      public async Task<IActionResult> GetUserSettingsAsync()
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
         var user = await UserService.ReadAsync(Guid.Parse(userId));

         return new OkObjectResult(
            new UserSettingsResponse(
             user.UserName,
             user.Email,
             user.IsPublic,
             user.PublicAlias,
             user.AllowAnonymousFiles,
             user.AllowAnonymousMessages,
             user.Created
         ));
      }

      [Authorize]
      [HttpGet("sent/messages")]
      public async Task<IActionResult> GetSentMessagesAsync()
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

         var sentMessagesSansRecipientInfo = (await MessageService.FindBySenderAsync(Guid.Parse(userId)))
            .Select(x => new { x.Id, x.Subject, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentMessages = new List<UserSentMessageDTO>();
         foreach (var item in sentMessagesSansRecipientInfo)
         {
            User recipient = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await UserService.ReadAsync(item.Recipient);
            }
            sentMessages.Add(new UserSentMessageDTO(item.Id, item.Subject, item.Recipient, recipient?.UserName, recipient?.PublicAlias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentMessagesResponse(sentMessages));
      }

      [Authorize]
      [HttpGet("sent/files")]
      public async Task<IActionResult> GetSentFilesAsync()
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

         var sentFilesSansRecipientInfo = (await FileService.FindBySenderAsync(Guid.Parse(userId)))
            .Select(x => new { x.Id, x.FileName, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentFiles = new List<UserSentFileDTO>();
         foreach (var item in sentFilesSansRecipientInfo)
         {
            User recipient = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await UserService.ReadAsync(item.Recipient);
            }
            sentFiles.Add(new UserSentFileDTO(item.Id, item.FileName, item.Recipient, recipient?.UserName, recipient?.PublicAlias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentFilesResponse(sentFiles));
      }

      [Authorize]
      [HttpGet("received/messages")]
      public async Task<IActionResult> GetReceivedMessagesAsync()
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

         var receivedMessagesSansSenderInfo = (await MessageService.FindByRecipientAsync(Guid.Parse(userId)))
            .Select(x => new { x.Id, x.Subject, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedMessages = new List<UserReceivedMessageDTO>();
         foreach (var item in receivedMessagesSansSenderInfo)
         {
            User sender = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await UserService.ReadAsync(item.Sender);
            }
            receivedMessages.Add(new UserReceivedMessageDTO(item.Id, item.Subject, item.Sender, sender?.UserName, sender?.PublicAlias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedMessagesResponse(receivedMessages));
      }

      [Authorize]
      [HttpGet("received/files")]
      public async Task<IActionResult> GetReceivedFilesAsync()
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

         var receivedFilesSansSenderInfo = (await FileService.FindByRecipientAsync(Guid.Parse(userId)))
            .Select(x => new { x.Id, x.FileName, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedFiles = new List<UserReceivedFileDTO>();
         foreach (var item in receivedFilesSansSenderInfo)
         {
            User sender = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await UserService.ReadAsync(item.Sender);
            }
            receivedFiles.Add(new UserReceivedFileDTO(item.Id, item.FileName, item.Sender, sender?.UserName, sender?.PublicAlias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedFilesResponse(receivedFiles));
      }

      [Authorize]
      [HttpPost("update-credentials")]
      public async Task<IActionResult> UpdateUserCredentialsAsync([FromBody] UpdateUserCredentialsRequest request)
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
         var user = await UserService.ReadAsync(Guid.Parse(userId));

         var updateResult = await UserService.UpdateCredentialsAsync(Guid.Parse(userId), user.UserName, request.Password);
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

      [Authorize]
      [HttpPost("update-privacy")]
      public async Task<IActionResult> UpdateUserPrivacyAsync([FromBody] UpdateUserPrivacyRequest request)
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;
         var user = await UserService.ReadAsync(Guid.Parse(userId));

         var updateResult = await UserService.UpdatePreferencesAsync(Guid.Parse(userId), request.PublicAlias, request.IsPublic, request.AllowAnonymousFiles, request.AllowAnonymousMessages);
         var responseObject = new UpdateUserPrivacyResponse(updateResult);

         if (updateResult == UpdateUserPreferencesResult.Success)
         {
            return new OkObjectResult(responseObject);
         }
         else
         {
            return new BadRequestObjectResult(responseObject);
         }
      }

      [Authorize]
      [HttpPost("update-personal-keys")]
      public async Task<IActionResult> UpdatePersonalKeysAsync([FromBody] UpdateUserKeysRequest body)
      {
         var userId = User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value;

         var insertResult = await KeyService.InsertUserPersonalKeyAsync(Guid.Parse(userId), body.EncryptedPrivateKeyBase64, body.PublicKey);
         if (insertResult)
         {
            return new OkObjectResult(
                new UpdateUserKeysResponse());
         }
         else
         {
            return new BadRequestObjectResult(
                new UpdateUserKeysResponse());
         }
      }

      [Authorize]
      [HttpGet("search/username")]
      public async Task<IActionResult> SearchByUsernameAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
      {
         var (total, users) = await UserService.SearchByUsernameAsync(value, index, count);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id.ToString(), x.UserName, x.PublicAlias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [Authorize]
      [HttpGet("search/public-alias")]
      public async Task<IActionResult> SearchByPublicAliasAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
      {
         var (total, users) = await UserService.SearchByPublicAliasAsync(value, index, count);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id.ToString(), x.UserName, x.PublicAlias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [HttpGet("{username}")]
      public async Task<IActionResult> GetPublicUserProfileAsync(string userName)
      {
         var profileIsPublic = await UserService.IsRegisteredUserPublicAsync(userName);
         if (profileIsPublic)
         {
            var user = await UserService.ReadPublicUserProfileInformation(userName);
            var publicKey = await KeyService.GetUserPublicKeyAsync(await UserService.UserIdFromUsernameAsync(userName));
            return new OkObjectResult(
                new UserPublicProfileResponse(user.UserName, user.PublicAlias, user.AllowAnonymousFiles, user.AllowAnonymousMessages, publicKey));
         }
         else
         {
            return new NotFoundObjectResult(
                new UserPublicProfileResponse(null, null, false, false, null));
         }
      }
   }
}
