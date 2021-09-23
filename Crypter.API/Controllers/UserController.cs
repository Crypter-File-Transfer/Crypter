using Crypter.API.Controllers.Methods;
using Crypter.API.Services;
using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
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
      private readonly IUserProfileService UserProfileService;
      private readonly IUserPublicKeyPairService<UserX25519KeyPair> UserDiffieHellmanKeyPairService;
      private readonly IUserPublicKeyPairService<UserEd25519KeyPair> UserDigitalSignatureKeyPairService;
      private readonly IUserSearchService UserSearchService;
      private readonly IUserPrivacyService UserPrivacyService;
      private readonly IBaseTransferService<MessageTransfer> MessageService;
      private readonly IBaseTransferService<FileTransfer> FileService;
      private readonly IBetaKeyService BetaKeyService;
      private readonly byte[] TokenSecretKey;

      public UserController(
          IUserService userService,
          IUserProfileService userProfileService,
          IUserPublicKeyPairService<UserX25519KeyPair> userDiffieHellmanKeyPairService,
          IUserPublicKeyPairService<UserEd25519KeyPair> userDigitalSignatureKeyPairService,
          IUserSearchService userSearchService,
          IUserPrivacyService userPrivacyService,
          IBaseTransferService<MessageTransfer> messageService,
          IBaseTransferService<FileTransfer> fileService,
          IBetaKeyService betaKeyService,
          IConfiguration configuration
          )
      {
         UserService = userService;
         UserProfileService = userProfileService;
         UserDiffieHellmanKeyPairService = userDiffieHellmanKeyPairService;
         UserDigitalSignatureKeyPairService = userDigitalSignatureKeyPairService;
         UserSearchService = userSearchService;
         UserPrivacyService = userPrivacyService;
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

         if (!ValidationService.IsValidPassword(request.Password))
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

         var userDHKeyPair = await UserDiffieHellmanKeyPairService.GetUserPublicKeyPairAsync(user.Id);
         var userDSAKeyPair = await UserDigitalSignatureKeyPairService.GetUserPublicKeyPairAsync(user.Id);

         return new OkObjectResult(
             new UserAuthenticateResponse(user.Id, tokenString, userDHKeyPair?.PrivateKey, userDSAKeyPair?.PrivateKey)
         );
      }

      [Authorize]
      [HttpGet("authenticate/refresh")]
      public IActionResult RefreshAuthenticationAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var tokenHandler = new JwtSecurityTokenHandler();
         var tokenDescriptor = new SecurityTokenDescriptor
         {
            Subject = new ClaimsIdentity(new Claim[]
            {
               new Claim(ClaimTypes.NameIdentifier, userId.ToString())
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
      [HttpGet("sent/messages")]
      public async Task<IActionResult> GetSentMessagesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var sentMessagesSansRecipientInfo = (await MessageService.FindBySenderAsync(userId))
            .Select(x => new { x.Id, x.Subject, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentMessages = new List<UserSentMessageDTO>();
         foreach (var item in sentMessagesSansRecipientInfo)
         {
            IUser recipient = null;
            IUserProfile recipientProfile = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await UserService.ReadAsync(item.Recipient);
               recipientProfile = await UserProfileService.ReadAsync(item.Recipient);
            }
            sentMessages.Add(new UserSentMessageDTO(item.Id, item.Subject, item.Recipient, recipient?.Username, recipientProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentMessagesResponse(sentMessages));
      }

      [Authorize]
      [HttpGet("sent/files")]
      public async Task<IActionResult> GetSentFilesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var sentFilesSansRecipientInfo = (await FileService.FindBySenderAsync(userId))
            .Select(x => new { x.Id, x.FileName, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentFiles = new List<UserSentFileDTO>();
         foreach (var item in sentFilesSansRecipientInfo)
         {
            IUser recipient = null;
            IUserProfile recipientProfile = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await UserService.ReadAsync(item.Recipient);
               recipientProfile = await UserProfileService.ReadAsync(item.Recipient);
            }
            sentFiles.Add(new UserSentFileDTO(item.Id, item.FileName, item.Recipient, recipient?.Username, recipientProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentFilesResponse(sentFiles));
      }

      [Authorize]
      [HttpGet("received/messages")]
      public async Task<IActionResult> GetReceivedMessagesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var receivedMessagesSansSenderInfo = (await MessageService.FindByRecipientAsync(userId))
            .Select(x => new { x.Id, x.Subject, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedMessages = new List<UserReceivedMessageDTO>();
         foreach (var item in receivedMessagesSansSenderInfo)
         {
            IUser sender = null;
            IUserProfile senderProfile = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await UserService.ReadAsync(item.Sender);
               senderProfile = await UserProfileService.ReadAsync(item.Sender);
            }
            receivedMessages.Add(new UserReceivedMessageDTO(item.Id, item.Subject, item.Sender, sender?.Username, senderProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedMessagesResponse(receivedMessages));
      }

      [Authorize]
      [HttpGet("received/files")]
      public async Task<IActionResult> GetReceivedFilesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var receivedFilesSansSenderInfo = (await FileService.FindByRecipientAsync(userId))
            .Select(x => new { x.Id, x.FileName, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedFiles = new List<UserReceivedFileDTO>();
         foreach (var item in receivedFilesSansSenderInfo)
         {
            IUser sender = null;
            IUserProfile senderProfile = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await UserService.ReadAsync(item.Sender);
               senderProfile = await UserProfileService.ReadAsync(item.Sender);
            }
            receivedFiles.Add(new UserReceivedFileDTO(item.Id, item.FileName, item.Sender, sender?.Username, senderProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedFilesResponse(receivedFiles));
      }

      [Authorize]
      [HttpGet("settings")]
      public async Task<IActionResult> GetUserSettingsAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);
         var user = await UserService.ReadAsync(userId);
         var userProfile = await UserProfileService.ReadAsync(userId);
         var userPrivacy = await UserPrivacyService.ReadAsync(userId);

         return new OkObjectResult(
            new UserSettingsResponse(
             user.Username,
             user.Email,
             userProfile?.Alias,
             userProfile?.About,
             userPrivacy?.Visibility ?? UserVisibilityLevel.None,
             userPrivacy?.AllowKeyExchangeRequests ?? false,
             userPrivacy?.ReceiveMessages ?? UserItemTransferPermission.None,
             userPrivacy?.ReceiveFiles ?? UserItemTransferPermission.None,
             user.Created
         ));
      }

      [Authorize]
      [HttpPost("settings/profile")]
      public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateProfileRequest request)
      {
         var userId = ClaimsParser.ParseUserId(User);
         var updateResult = await UserProfileService.UpdateAsync(userId, request.Alias, request.About);
         var responseObject = new UpdateProfileResponse();

         if (updateResult)
         {
            return new OkObjectResult(responseObject);
         }
         else
         {
            return new BadRequestObjectResult(responseObject);
         }
      }

      [Authorize]
      [HttpPost("settings/contact")]
      public async Task<IActionResult> UpdateUserContactInfoAsync([FromBody] UpdateContactInfoRequest request)
      {
         var userId = ClaimsParser.ParseUserId(User);
         var updateResult = await UserService.UpdateContactInfoAsync(userId, request.Email, request.CurrentPassword);
         var responseObject = new UpdateContactInfoResponse(updateResult);

         if (updateResult == UpdateContactInfoResult.Success)
         {
            return new OkObjectResult(responseObject);
         }
         else
         {
            return new BadRequestObjectResult(responseObject);
         }
      }

      [Authorize]
      [HttpPost("settings/privacy")]
      public async Task<IActionResult> UpdateUserPrivacyAsync([FromBody] UpdatePrivacyRequest request)
      {
         var userId = ClaimsParser.ParseUserId(User);
         var updateSuccess = await UserPrivacyService.UpsertAsync(userId, request.AllowKeyExchangeRequests, request.VisibilityLevel, request.FileTransferPermission, request.MessageTransferPermission);

         if (updateSuccess)
         {
            return new OkObjectResult(new UpdatePrivacyResponse());
         }
         else
         {
            return new BadRequestObjectResult(new UpdatePrivacyResponse());
         }
      }

      [Authorize]
      [HttpPost("settings/keys/x25519")]
      public async Task<IActionResult> UpdateDiffieHellmanKeysAsync([FromBody] UpdateKeysRequest body)
      {
         var userId = ClaimsParser.ParseUserId(User);

         var insertResult = await UserDiffieHellmanKeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKey);
         if (insertResult)
         {
            return new OkObjectResult(
                new UpdateKeysResponse());
         }
         else
         {
            return new BadRequestObjectResult(
                new UpdateKeysResponse());
         }
      }

      [Authorize]
      [HttpPost("settings/keys/ed25519")]
      public async Task<IActionResult> UpdateDigitalSignatureKeysAsync([FromBody] UpdateKeysRequest body)
      {
         var userId = ClaimsParser.ParseUserId(User);

         var insertResult = await UserDigitalSignatureKeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKey);
         if (insertResult)
         {
            return new OkObjectResult(
                new UpdateKeysResponse());
         }
         else
         {
            return new BadRequestObjectResult(
                new UpdateKeysResponse());
         }
      }

      [Authorize]
      [HttpGet("search/username")]
      public async Task<IActionResult> SearchByUsernameAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
      {
         var searchPartyId = ClaimsParser.ParseUserId(User);
         var (total, users) = await UserSearchService.SearchByUsernameAsync(searchPartyId, value, index, count);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id, x.Username, x.Alias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [Authorize]
      [HttpGet("search/public-alias")]
      public async Task<IActionResult> SearchByAliasAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
      {
         var searchPartyId = ClaimsParser.ParseUserId(User);
         var (total, users) = await UserSearchService.SearchByAliasAsync(searchPartyId, value, index, count);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id, x.Username, x.Alias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [HttpGet("{username}")]
      public async Task<IActionResult> GetPublicUserProfileAsync(string username)
      {
         var visitorId = ClaimsParser.ParseUserId(User);
         var notFoundResponse = new UserPublicProfileResponse(default, null, null, null, false, false, false, false, null, null);

         var requestor = ClaimsParser.ParseUserId(User);
         var user = await UserService.ReadAsync(username);
         if (user is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userProfile = await UserProfileService.ReadAsync(user.Id);
         if (userProfile is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userPrivacy = await UserPrivacyService.ReadAsync(user.Id);
         if (userPrivacy is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userAllowsRequestorToViewProfile = await UserPrivacyService.IsUserViewableByPartyAsync(user.Id, requestor);
         if (userAllowsRequestorToViewProfile)
         {
            var userPublicDHKey = await UserDiffieHellmanKeyPairService.GetUserPublicKeyAsync(user.Id);
            var userPublicDSAKey = await UserDigitalSignatureKeyPairService.GetUserPublicKeyAsync(user.Id);

            var visitorCanSendMessages = await UserPrivacyService.DoesUserAcceptMessagesFromOtherPartyAsync(user.Id, visitorId);
            var visitorCanSendFiles = await UserPrivacyService.DoesUserAcceptFilesFromOtherPartyAsync(user.Id, visitorId);

            return new OkObjectResult(
               new UserPublicProfileResponse(user.Id, user.Username, userProfile.Alias, userProfile.About, userPrivacy.AllowKeyExchangeRequests,
               true, visitorCanSendMessages, visitorCanSendFiles, userPublicDHKey, userPublicDSAKey));
         }
         else
         {
            return new NotFoundObjectResult(notFoundResponse);
         }
      }
   }
}
